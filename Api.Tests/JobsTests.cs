// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Diagnostics;
using System.Text.Json;
using Api.Database;
using Api.Database.Utils;
using Api.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using Npgsql;
using JobEntity = Api.Jobs.Models.JobData;
using JobState = Api.Jobs.Models.JobState;

namespace Api.Tests;

/// <summary>
/// Covers the Jobs queue operations against the real PostgreSQL schema.
///
/// The Jobs slice is not wired into the schema/DI yet (<c>Api.Jobs.Setup</c> is never called and
/// <c>[GraphQLIgnore]</c>d DTOs keep it out of the graph), so these tests construct
/// <see cref="JobsInterfae"/> from the test host's services instead of resolving it.
///
/// Tests run in parallel against one schema, so every test filters by a job type it generates
/// itself; only the explicit "no filter" assertions may observe rows from other tests.
/// </summary>
public class JobsTests : TestInit
{
    static readonly Duration LeaseLength = Duration.FromMinutes(5);

    static Instant Now => SystemClock.Instance.GetCurrentInstant();

    static string UniqueType() => $"test.{Guid.CreateVersion7():n}.v1";

    static JobEntity Job(
        string jobType,
        int maxRetries = 3,
        int currentTry = 0,
        JobState state = JobState.New,
        Instant? leaseEnd = null,
        Guid? workerId = null,
        Instant? completionTime = null
    ) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            JobType = jobType,
            LeaseLength = LeaseLength,
            MaxRetries = maxRetries,
            CurrentTry = currentTry,
            JobState = state,
            LeaseEnd = leaseEnd,
            WorkerId = workerId,
            CompletionTime = completionTime,
        };

    sealed class JobFixture : IAsyncDisposable
    {
        readonly AsyncServiceScope _scope;

        public JobFixture(IServiceProvider services)
        {
            _scope = services.CreateAsyncScope();
            Db = _scope.ServiceProvider.GetRequiredService<PGContext>();
            Tx =
                _scope.ServiceProvider.GetRequiredService<IEFTransactionDIAccessorService>();
            Notifier = new JobNotifier(
                services.GetRequiredService<IConfiguration>(),
                services.GetRequiredService<IHostApplicationLifetime>()
            );
            Jobs = new JobsInterfae(Db, Notifier, Tx);
        }

        public PGContext Db { get; }
        public IEFTransactionDIAccessorService Tx { get; }
        public JobNotifier Notifier { get; }
        public JobsInterfae Jobs { get; }

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }

    JobFixture Fixture() => new(Factory.Services);

    static async Task Seed(JobFixture f, CancellationToken ct, params JobEntity[] jobs)
    {
        await f.Db.JobDatas.AddRangeAsync(jobs, ct);
        await f.Db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Commits like a caller would and then forgets the tracked instances, so the next service
    /// call reads the committed rows (production gives every call a fresh scope).
    /// </summary>
    static async Task Commit(JobFixture f, CancellationToken ct)
    {
        await f.Tx.SaveAndCommitTX(ct);
        f.Db.ChangeTracker.Clear();
    }

    static async Task<JobEntity> Reload(JobFixture f, Guid id, CancellationToken ct) =>
        await f.Db.JobDatas.AsNoTracking().SingleAsync(j => j.Id == id, ct);

    [Test]
    public async Task PeekPendingJobs_ReturnsOnlyLeasableJobs(CancellationToken ct)
    {
        await using var f = Fixture();
        var type = UniqueType();
        var now = Now;

        var free = Job(type);
        var failed = Job(type, state: JobState.FailedCompletion, currentTry: 1);
        var expired = Job(
            type,
            state: JobState.HaveOrHadLease,
            leaseEnd: now - Duration.FromSeconds(1),
            workerId: Guid.CreateVersion7(),
            currentTry: 1
        );

        await Seed(
            f,
            ct,
            free,
            failed,
            expired,
            Job(type, state: JobState.Completed, completionTime: now),
            Job(type, maxRetries: 3, currentTry: 4),
            Job(
                type,
                state: JobState.HaveOrHadLease,
                leaseEnd: now + Duration.FromMinutes(1),
                workerId: Guid.CreateVersion7(),
                currentTry: 1
            )
        );

        var peeked = await f.Jobs.PeekPendingJobs(0, null, null, [type], ct);

        await Assert.That(peeked.Length).IsEqualTo(3);
        await Assert.That(peeked.Select(j => j.JobId)).Contains(free.Id);
        await Assert.That(peeked.Select(j => j.JobId)).Contains(expired.Id);
        await Assert.That(peeked.Select(j => j.JobId)).Contains(failed.Id);
        await Assert.That(peeked.All(j => j.JobType == type)).IsTrue();
    }

    [Test]
    public async Task PeekPendingJobs_ReturnsFailedCompletionJobsForRetry(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        var now = Now;

        var failed = Job(
            type,
            state: JobState.FailedCompletion,
            completionTime: now,
            currentTry: 1
        );
        var failedButLeased = Job(
            type,
            state: JobState.FailedCompletion,
            completionTime: now,
            leaseEnd: now + Duration.FromMinutes(1),
            workerId: Guid.CreateVersion7(),
            currentTry: 1
        );

        await Seed(f, ct, failed, failedButLeased);

        var peeked = await f.Jobs.PeekPendingJobs(0, null, null, [type], ct);

        await Assert.That(peeked.Length).IsEqualTo(1);
        await Assert.That(peeked.Single().JobId).IsEqualTo(failed.Id);
    }

    [Test]
    public async Task PeekPendingJobs_DoesNotLeaseOrMutateAnything(CancellationToken ct)
    {
        await using var f = Fixture();
        var type = UniqueType();
        var job = Job(type);

        await Seed(f, ct, job);

        var peeked = await f.Jobs.PeekPendingJobs(0, null, null, [type], ct);

        await Assert.That(peeked.Length).IsEqualTo(1);

        var stored = await Reload(f, job.Id, ct);
        await Assert.That(stored.JobState).IsEqualTo(JobState.New);
        await Assert.That(stored.CurrentTry).IsEqualTo(0);
        await Assert.That(stored.WorkerId).IsNull();
        await Assert.That(stored.LeaseEnd).IsNull();
        await Assert.That(stored.LeasedAt).IsNull();
    }

    [Test]
    public async Task PeekPendingJobs_HonoursMaxJobsDeterministically(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();

        await Seed(f, ct, Job(type), Job(type), Job(type));

        var unlimited = await f.Jobs.PeekPendingJobs(0, null, null, [type], ct);
        var first = await f.Jobs.PeekPendingJobs(0, 1, null, [type], ct);
        var second = await f.Jobs.PeekPendingJobs(0, 1, null, [type], ct);
        var two = await f.Jobs.PeekPendingJobs(0, 2, null, [type], ct);

        await Assert.That(unlimited.Length).IsEqualTo(3);
        await Assert.That(first.Length).IsEqualTo(1);
        await Assert.That(second.Single().JobId).IsEqualTo(first.Single().JobId);
        await Assert.That(two.Length).IsEqualTo(2);
    }

    [Test]
    public async Task PeekPendingJobs_FiltersByJobType(CancellationToken ct)
    {
        await using var f = Fixture();
        var wanted = UniqueType();
        var other = UniqueType();

        var target = Job(wanted);
        var foreign = Job(other);

        await Seed(f, ct, target, foreign);

        var mine = await f.Jobs.PeekPendingJobs(0, null, null, [wanted], ct);
        var none = await f.Jobs.PeekPendingJobs(0, null, null, [UniqueType()], ct);
        var unfiltered = await f.Jobs.PeekPendingJobs(0, null, null, null, ct);

        await Assert.That(mine.Length).IsEqualTo(1);
        await Assert.That(mine.Single().JobId).IsEqualTo(target.Id);
        await Assert.That(none).IsEmpty();
        await Assert.That(unfiltered.Select(j => j.JobId)).Contains(target.Id);
        await Assert.That(unfiltered.Select(j => j.JobId)).Contains(foreign.Id);
    }

    [Test]
    public async Task PeekPendingJobs_EmptyJobTypeFilterReturnsNothing(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        await Seed(f, ct, Job(UniqueType()));

        var peeked = await f.Jobs.PeekPendingJobs(0, null, null, [], ct);

        await Assert.That(peeked).IsEmpty();
    }

    [Test]
    public async Task PeekPendingJobs_WithPendingJobsReturnsWithoutWaiting(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        await Seed(f, ct, Job(type));

        var stopwatch = Stopwatch.StartNew();
        var peeked = await f.Jobs.PeekPendingJobs(30_000, null, null, [type], ct);
        stopwatch.Stop();

        await Assert.That(peeked.Length).IsEqualTo(1);
        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task LeasePendingJobs_LeasesLeasableJobsOnlyOnce(CancellationToken ct)
    {
        await using var f = Fixture();
        var type = UniqueType();
        var worker = Guid.CreateVersion7();

        var leasable = Job(type);
        await Seed(
            f,
            ct,
            leasable,
            Job(type, state: JobState.Completed, completionTime: Now)
        );

        var before = Now;
        var leased = await f.Jobs.LeasePendingJobs(0, null, worker, [type], ct);
        await Commit(f, ct);

        await Assert.That(leased.Length).IsEqualTo(1);
        await Assert.That(leased.Single().JobId).IsEqualTo(leasable.Id);

        var stored = await Reload(f, leasable.Id, ct);
        await Assert.That(stored.JobState).IsEqualTo(JobState.HaveOrHadLease);
        await Assert.That(stored.WorkerId).IsEqualTo(worker);
        await Assert.That(stored.CurrentTry).IsEqualTo(1);
        await Assert.That(stored.LeasedAt!.Value).IsGreaterThanOrEqualTo(before);
        await Assert
            .That(stored.LeaseEnd)
            .IsEqualTo(stored.LeasedAt!.Value + LeaseLength);

        var again = await f.Jobs.LeasePendingJobs(0, null, worker, [type], ct);
        await Commit(f, ct);

        await Assert.That(again).IsEmpty();
    }

    [Test]
    public async Task LeasePendingJobs_SkipsActiveLeasesAndReclaimsExpiredOnes(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        var worker = Guid.CreateVersion7();
        var now = Now;

        var expired = Job(
            type,
            state: JobState.HaveOrHadLease,
            leaseEnd: now - Duration.FromSeconds(1),
            workerId: Guid.CreateVersion7(),
            currentTry: 1
        );
        var active = Job(
            type,
            state: JobState.HaveOrHadLease,
            leaseEnd: now + Duration.FromMinutes(1),
            workerId: Guid.CreateVersion7(),
            currentTry: 1
        );
        var outOfRetries = Job(type, currentTry: 4, maxRetries: 3);

        await Seed(f, ct, expired, active, outOfRetries);

        var leased = await f.Jobs.LeasePendingJobs(0, null, worker, [type], ct);
        await Commit(f, ct);

        await Assert.That(leased.Length).IsEqualTo(1);
        await Assert.That(leased.Single().JobId).IsEqualTo(expired.Id);
        await Assert.That((await Reload(f, expired.Id, ct)).WorkerId).IsEqualTo(worker);
        await Assert.That((await Reload(f, active.Id, ct)).WorkerId).IsNotEqualTo(worker);
        await Assert.That((await Reload(f, outOfRetries.Id, ct)).LeaseEnd).IsNull();
    }

    [Test]
    public async Task LeasePendingJobs_HonoursJobTypeFilter(CancellationToken ct)
    {
        await using var f = Fixture();
        var wanted = UniqueType();
        var mine = Job(wanted);
        await Seed(f, ct, mine, Job(UniqueType()));

        var leased = await f.Jobs.LeasePendingJobs(
            0,
            null,
            Guid.CreateVersion7(),
            [wanted],
            ct
        );
        await Commit(f, ct);

        await Assert.That(leased.Length).IsEqualTo(1);
        await Assert.That(leased.Single().JobId).IsEqualTo(mine.Id);
    }

    [Test]
    public async Task MarkJobsAsComplete_CompletesLeasedJobAndStoresResult(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        var worker = Guid.CreateVersion7();
        var job = Job(type);
        await Seed(f, ct, job);
        await f.Jobs.LeasePendingJobs(0, null, worker, [type], ct);
        await Commit(f, ct);

        var before = Now;
        var results = await f.Jobs.MarkJobsAsComplete(
            [new JobCompletionData(job.Id, JsonDocument.Parse("""{"pages":42}"""))],
            worker,
            ct
        );
        await Commit(f, ct);

        await Assert.That(results[job.Id]).IsEqualTo(JobMarkAsCompleteResult.Completed);

        var stored = await Reload(f, job.Id, ct);
        await Assert.That(stored.JobState).IsEqualTo(JobState.Completed);
        await Assert.That(stored.CompletionTime!.Value).IsGreaterThanOrEqualTo(before);
        await Assert
            .That(stored.JobResult!.RootElement.GetProperty("pages").GetInt32())
            .IsEqualTo(42);
    }

    [Test]
    public async Task MarkJobsAsComplete_ReportsAlreadyCompletedOnSecondCall(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        var worker = Guid.CreateVersion7();
        var job = Job(type);
        await Seed(f, ct, job);
        await f.Jobs.LeasePendingJobs(0, null, worker, [type], ct);
        await Commit(f, ct);
        await f.Jobs.MarkJobsAsComplete(
            [new JobCompletionData(job.Id, null)],
            worker,
            ct
        );
        await Commit(f, ct);

        var stored = await Reload(f, job.Id, ct);

        var second = await f.Jobs.MarkJobsAsComplete(
            [new JobCompletionData(job.Id, JsonDocument.Parse("""{"pages":1}"""))],
            worker,
            ct
        );
        await Commit(f, ct);

        await Assert
            .That(second[job.Id])
            .IsEqualTo(JobMarkAsCompleteResult.AlreadyCompleted);
        await Assert
            .That((await Reload(f, job.Id, ct)).CompletionTime)
            .IsEqualTo(stored.CompletionTime);
        await Assert.That((await Reload(f, job.Id, ct)).JobResult).IsNull();
    }

    [Test]
    public async Task MarkJobsAsComplete_ReportsForeignWorkersExpiredLeasesAndUnknownJobs(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        var owner = Guid.CreateVersion7();
        var intruder = Guid.CreateVersion7();
        var now = Now;

        var foreign = Job(
            type,
            state: JobState.HaveOrHadLease,
            leaseEnd: now + Duration.FromMinutes(1),
            workerId: owner,
            currentTry: 1
        );
        var lapsed = Job(
            type,
            state: JobState.HaveOrHadLease,
            leaseEnd: now - Duration.FromMinutes(1),
            workerId: owner,
            currentTry: 1
        );
        var unknown = Guid.CreateVersion7();

        await Seed(f, ct, foreign, lapsed);

        var results = await f.Jobs.MarkJobsAsComplete(
            [
                new JobCompletionData(foreign.Id, null),
                new JobCompletionData(lapsed.Id, null),
                new JobCompletionData(unknown, null),
            ],
            intruder,
            ct
        );
        await Commit(f, ct);

        await Assert
            .That(results[foreign.Id])
            .IsEqualTo(JobMarkAsCompleteResult.LeasedBySomeoneElse);
        await Assert
            .That(results[lapsed.Id])
            .IsEqualTo(JobMarkAsCompleteResult.NotLeasedCurrently);
        await Assert
            .That(results[unknown])
            .IsEqualTo(JobMarkAsCompleteResult.JobDoesNotExist);
        await Assert
            .That((await Reload(f, foreign.Id, ct)).JobState)
            .IsEqualTo(JobState.HaveOrHadLease);
        await Assert
            .That((await Reload(f, lapsed.Id, ct)).JobState)
            .IsEqualTo(JobState.HaveOrHadLease);
    }

    [Test]
    public async Task MarkJobsAsComplete_CompletesForAnyWorkerWhenNoneIsGiven(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        var owner = Guid.CreateVersion7();
        var job = Job(type);
        await Seed(f, ct, job);
        await f.Jobs.LeasePendingJobs(0, null, owner, [type], ct);
        await Commit(f, ct);

        var results = await f.Jobs.MarkJobsAsComplete(
            [new JobCompletionData(job.Id, null)],
            null,
            ct
        );
        await Commit(f, ct);

        await Assert.That(results[job.Id]).IsEqualTo(JobMarkAsCompleteResult.Completed);
    }

    [Test]
    public async Task WaitPendingJobs_ReportsWhetherJobsArePending(CancellationToken ct)
    {
        await using var f = Fixture();
        var type = UniqueType();
        await Seed(f, ct, Job(type, state: JobState.Completed, completionTime: Now));

        var completedOnly = await f.Jobs.WaitPendingJobs(0, null, [type], ct);
        await Seed(f, ct, Job(type));
        var withPending = await f.Jobs.WaitPendingJobs(0, null, [type], ct);

        await Assert.That(completedOnly).IsFalse();
        await Assert.That(withPending).IsTrue();
    }

    [Test]
    public async Task WaitForCompletedJobsOrForSeconds_ReturnsJobsAfterTheGivenTime(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        var now = Now;

        var old = Job(
            type,
            state: JobState.Completed,
            completionTime: now - Duration.FromHours(1)
        );
        var recent = Job(
            type,
            state: JobState.Completed,
            completionTime: now - Duration.FromMinutes(1)
        );
        await Seed(f, ct, old, recent);

        var result = await f.Jobs.WaitForCompletedJobsOrForSeconds(
            0,
            now - Duration.FromMinutes(10),
            null,
            [type],
            ct
        );

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.JobIds).Contains(recent.Id);
        await Assert.That(result.JobIds).DoesNotContain(old.Id);
        await Assert
            .That(result.LatestCompletedJobTime.ToUnixTimeMilliseconds())
            .IsEqualTo(recent.CompletionTime!.Value.ToUnixTimeMilliseconds());

        var afterCursor = await f.Jobs.WaitForCompletedJobsOrForSeconds(
            200,
            result.LatestCompletedJobTime,
            null,
            [type],
            ct
        );

        await Assert.That(afterCursor).IsNull();
    }

    [Test]
    public async Task WaitForCompletedJobsOrForSeconds_HonoursMaxJobsJobIdsAndJobTypes(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var wanted = UniqueType();
        var other = UniqueType();
        var now = Now;

        var target = Job(
            wanted,
            state: JobState.Completed,
            completionTime: now - Duration.FromMinutes(1)
        );
        var foreign = Job(
            other,
            state: JobState.Completed,
            completionTime: now - Duration.FromMinutes(2)
        );
        await Seed(f, ct, target, foreign);

        var byType = await f.Jobs.WaitForCompletedJobsOrForSeconds(
            0,
            now - Duration.FromHours(1),
            null,
            [wanted],
            ct
        );
        var byId = await f.Jobs.WaitForCompletedJobsOrForSeconds(
            0,
            now - Duration.FromHours(1),
            null,
            null,
            ct,
            [foreign.Id]
        );
        var limited = await f.Jobs.WaitForCompletedJobsOrForSeconds(
            0,
            now - Duration.FromHours(1),
            1,
            [wanted],
            ct
        );
        var noIds = await f.Jobs.WaitForCompletedJobsOrForSeconds(
            0,
            now - Duration.FromHours(1),
            null,
            null,
            ct,
            []
        );

        await Assert.That(byType!.JobIds.Length).IsEqualTo(1);
        await Assert.That(byType.JobIds).Contains(target.Id);
        await Assert.That(byId!.JobIds.Length).IsEqualTo(1);
        await Assert.That(byId.JobIds).Contains(foreign.Id);
        await Assert.That(limited!.JobIds.Length).IsEqualTo(1);
        await Assert.That(noIds).IsNull();
    }

    [Test]
    public async Task WaitForCompletedJobsOrForSeconds_ReturnsNullWhenTimeoutPasses(
        CancellationToken ct
    )
    {
        await using var f = Fixture();
        var type = UniqueType();
        await Seed(f, ct, Job(type));

        var result = await f.Jobs.WaitForCompletedJobsOrForSeconds(
            200,
            Now,
            null,
            [type],
            ct
        );

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task WaitForCompletedJobsOrForSeconds_ReturnsJobCompletedWhileWaiting(
        CancellationToken ct
    )
    {
        await using var waiter = Fixture();
        await using var actor = Fixture();
        var type = UniqueType();
        var worker = Guid.CreateVersion7();

        await ListenUntilReady(waiter, ct);

        var after = Now;
        var waiting = waiter.Jobs.WaitForCompletedJobsOrForSeconds(
            0,
            after,
            null,
            [type],
            ct
        );
        await Task.Delay(500, ct);

        var job = Job(type);
        await Seed(actor, ct, job);
        await actor.Jobs.LeasePendingJobs(0, null, worker, [type], ct);
        await Commit(actor, ct);
        var marked = await actor.Jobs.MarkJobsAsComplete(
            [new JobCompletionData(job.Id, null)],
            worker,
            ct
        );
        await Commit(actor, ct);
        await Assert.That(marked[job.Id]).IsEqualTo(JobMarkAsCompleteResult.Completed);

        var result = await waiting.WaitAsync(TimeSpan.FromSeconds(15), ct);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.JobIds).Contains(job.Id);
        await Assert.That(result.LatestCompletedJobTime).IsGreaterThanOrEqualTo(after);
    }

    /// <summary>
    /// <see cref="JobNotifier"/> registers its LISTEN in a background loop, and PostgreSQL only
    /// delivers notifications issued after that registration.
    /// </summary>
    static async Task ListenUntilReady(JobFixture f, CancellationToken ct)
    {
        var channel = $"{f.Db.SchemaName}_job_completed";

        await using var conn = new NpgsqlConnection(
            f.Db.Database.GetConnectionString()
                ?? throw new InvalidOperationException("no PG")
        );
        await conn.OpenAsync(ct);

        for (var attempt = 0; attempt < 100; attempt++)
        {
            await using var cmd = new NpgsqlCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE query LIKE '%' || @channel || '%'",
                conn
            );
            cmd.Parameters.AddWithValue("channel", channel);

            if ((long)(await cmd.ExecuteScalarAsync(ct))! > 0)
            {
                return;
            }

            await Task.Delay(100, ct);
        }

        throw new InvalidOperationException("JobNotifier never registered its LISTEN");
    }
}
// Mostly Ai Generated - End
