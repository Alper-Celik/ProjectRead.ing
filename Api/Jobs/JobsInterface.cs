// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Text.Json;

using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using Api.Jobs.Models;

using Microsoft.EntityFrameworkCore;

using NodaTime;

using Npgsql;
using NpgsqlTypes;

namespace Api.Jobs;

public class JobsInterfae(
    PGContext db,
    JobNotifier jobNotifier,
    IEFTransactionDIAccessorService tx
) : IJobsInterfae
{
    // Mostly Ai Generated - Start
    string Schema => db.Model.GetDefaultSchema()!;

    const string PendingJobsWhere = """
            (lease_end IS NULL OR lease_end < @now)
            AND job_state <> @completed_state
            AND current_try <= max_retries
            AND (@job_types IS NULL OR job_type = ANY(@job_types))
        """;

    static string LeasePendingJobsSql(string schema) =>
            $"""
            UPDATE "{schema}".job_datas j
            SET leased_at = @now,
                lease_end = @now + lease_length,
                worker_id = @worker_id,
                job_state = @leased_state,
                current_try = current_try + 1
            WHERE j.id in (
                SELECT id FROM "{schema}".job_datas
                WHERE
                {PendingJobsWhere}
                ORDER BY id
                LIMIT @max_jobs
                FOR UPDATE SKIP LOCKED
                )
            RETURNING j.*
            """;

    static string PeekPendingJobsSql(string schema) =>
            $"""
            SELECT * FROM "{schema}".job_datas
            WHERE
            {PendingJobsWhere}
            """;

    static NpgsqlParameter JobTypesParameter(string[]? jobTypes) =>
        new("job_types", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = (object?)jobTypes ?? DBNull.Value,
        };

    static int MaxJobsLimit(uint? maxJobs) =>
        maxJobs is null ? int.MaxValue : (int)Math.Min((long)maxJobs.Value, int.MaxValue);
    // Mostly Ai Generated - End

    public async Task<Dictionary<Guid, IExtendLeaseResult>> ExtendJobLeases(
        Guid[] jobIds,
        Guid? workerId,
        CancellationToken ct
    )
    {
        await tx.BeginOrGetTransactionAsync();
        var now = Now();
        var jobs = await db.JobDatas.Where(j => jobIds.Contains(j.Id)).ToArrayAsync(ct);

        Dictionary<Guid, IExtendLeaseResult> result = jobs.Distinct()
            .ToDictionary(
                j => j.Id,
                j =>
                    (IExtendLeaseResult)
                        new LeaseExtendFailed(LeaseExtendFailReason.NotLeasedCurrently)
            );

        foreach (var job in jobs)
        {
            IExtendLeaseResult value;

            if (job.LeaseEnd < now)
            {
                value = new LeaseExtendFailed(LeaseExtendFailReason.NotLeasedCurrently);
                result[job.Id] = value;
                continue;
            }

            if (job.CurrentLeaseExtension >= job.MaxLeaseExtensions)
            {
                value = new LeaseExtendFailed(
                    LeaseExtendFailReason.LeaseExtensionLimitReached
                );
                result[job.Id] = value;
                continue;
            }

            if (workerId is not null && job.WorkerId != workerId)
            {
                value = new LeaseExtendFailed(LeaseExtendFailReason.LeasedBySomeoneElse);
                result[job.Id] = value;
                continue;
            }

            job.CurrentLeaseExtension++;
            job.LeaseEnd = now + job.LeaseLength;
        }

        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<Dictionary<Guid, ILeaseJobResult>> LeaseJobs(
        Guid[] jobIds,
        Guid? workerId,
        CancellationToken ct
    )
    {
        await tx.BeginOrGetTransactionAsync(ct);
        var jobs = await db.JobDatas.Where(j => jobIds.Contains(j.Id)).ToArrayAsync(ct);

        var result = jobs.Distinct()
            .ToDictionary(
                j => j.Id,
                j => (ILeaseJobResult)new LeasingFailed(LeasingFailReason.JobDoesNotExist)
            );

        var now = Now();

        foreach (var job in jobs)
        {
            if (job.LeaseEnd > now)
            {
                result[job.Id] = new LeasingFailed(LeasingFailReason.CurrentlyLeased);
                continue;
            }

            if (job.JobState == JobState.Completed)
            {
                result[job.Id] = new LeasingFailed(LeasingFailReason.AlreadyCompleted);
                continue;
            }

            if (job.MaxRetries < job.CurrentTry)
            {
                result[job.Id] = new LeasingFailed(LeasingFailReason.MaxRetryReached);
                continue;
            }

            job.WorkerId = workerId;
            job.JobState = JobState.HaveOrHadLease;
            job.CurrentTry++;
            job.LeasedAt = now;
            job.LeaseEnd = now + job.LeaseLength;

            result[job.Id] = new NewLeaseEndTime(job.LeaseEnd.Value);
        }

        await db.SaveChangesAsync(ct);

        return result;
    }

    public async Task<JobData[]> LeasePendingJobs(
        uint timeout,
        uint? maxJobs,
        Guid? workerId,
        string[]? jobTypes,
        CancellationToken ct
    )
    {
        if (jobTypes is not null && jobTypes.Length == 0)
        {
            return [];
        }
        JobData[] jobs = [];

        if (timeout == 0)
        {
            await tx.BeginOrGetTransactionAsync();
            var now = Now();

            // Mostly Ai Generated - Start
            // UPDATE ... RETURNING is not composable, so the projection stays client-side
            var leased = await db
                .JobDatas.FromSqlRaw(
                    LeasePendingJobsSql(Schema),
                    new NpgsqlParameter("now", now),
                    new NpgsqlParameter("worker_id", workerId),
                    new NpgsqlParameter("leased_state", (int)Models.JobState.HaveOrHadLease),
                    new NpgsqlParameter("completed_state", (int)Models.JobState.Completed),
                    JobTypesParameter(jobTypes),
                    new NpgsqlParameter("max_jobs", MaxJobsLimit(maxJobs))
                )
                .AsNoTracking()
                .ToArrayAsync(ct);

            return leased.Select(j => new JobData(j.Id, j.JobType)).ToArray();
            // Mostly Ai Generated - End
        }

        if (timeout != 0)
        {
            jobs = await LeasePendingJobs(0, maxJobs, workerId, jobTypes, ct);
        }

        if (jobs.Length > 0)
        {
            return jobs;
        }

        var newJob = await jobNotifier.ListenForNewJob((int)timeout, ct, jobTypes);

        if (newJob != null)
        {
            return await LeasePendingJobs(0, maxJobs, workerId, jobTypes, ct);
        }

        return jobs;
    }

    // Mostly Ai Generated - Start
    public async Task<Dictionary<Guid, JobMarkAsCompleteResult>> MarkJobsAsComplete(
        JobCompletionData[] jobCompletions,
        Guid? workerId,
        CancellationToken ct
    )
    {
        await tx.BeginOrGetTransactionAsync(ct);

        var completions = jobCompletions.DistinctBy(c => c.JobId).ToArray();
        var jobIds = completions.Select(c => c.JobId).ToArray();
        var completionData = completions.ToDictionary(c => c.JobId, c => c.CompletionData);

        var jobs = await db.JobDatas.Where(j => jobIds.Contains(j.Id)).ToArrayAsync(ct);

        var result = jobIds.ToDictionary(id => id, _ => JobMarkAsCompleteResult.JobDoesNotExist);

        var now = Now();

        foreach (var job in jobs)
        {
            if (job.JobState == JobState.Completed)
            {
                result[job.Id] = JobMarkAsCompleteResult.AlreadyCompleted;
                continue;
            }

            if (job.LeaseEnd is null || job.LeaseEnd < now)
            {
                result[job.Id] = JobMarkAsCompleteResult.NotLeasedCurrently;
                continue;
            }

            if (workerId is not null && job.WorkerId != workerId)
            {
                result[job.Id] = JobMarkAsCompleteResult.LeasedBySomeoneElse;
                continue;
            }

            job.JobResult = completionData[job.Id];
            job.JobState = JobState.Completed;
            job.CompletionTime = now;
            await NotifyJobCompleted(job, ct);

            result[job.Id] = JobMarkAsCompleteResult.Completed;
        }

        await db.SaveChangesAsync(ct);
        return result;
    }

    // pg_notify is delivered by PostgreSQL on commit, so the notification only reaches other
    // instances (and their JobNotifier listeners) once the caller commits the transaction
    async Task NotifyJobCompleted(Models.JobData job, CancellationToken ct) =>
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_notify(@channel, @payload)",
            new object[]
            {
                new NpgsqlParameter("channel", $"{Schema}_job_completed"),
                new NpgsqlParameter(
                    "payload",
                    JsonSerializer.Serialize(new JobData(job.Id, job.JobType))
                ),
            },
            ct
        );
    // Mostly Ai Generated - End

    public async Task<Dictionary<Guid, IPeekJobResult>> PeekJobs(
        Guid[] jobIds,
        Guid? workerId,
        CancellationToken ct
    )
    {
        var query = db.JobDatas.Where(j => jobIds.Contains(j.Id));
        if (workerId is not null)
        {
            query = query.Where(j => j.WorkerId == null || j.WorkerId == workerId);
        }

        var jobs = await query
            .Select(j => new JobData(j.Id, j.JobType))
            .ToArrayAsync(ct);

        var result = jobIds
            .Distinct()
            .ToDictionary(id => id, _ => (IPeekJobResult)new JobDoesNotExists());

        foreach (var job in jobs)
        {
            result[job.JobId] = new Success(job);
        }

        return result;
    }

    // Mostly Ai Generated - Start
    public async Task<JobData[]> PeekPendingJobs(
        uint timeout,
        uint? maxJobs,
        Guid? workerId,
        string[]? jobTypes,
        CancellationToken ct
    )
    {
        if (jobTypes is not null && jobTypes.Length == 0)
        {
            return [];
        }

        var jobs = await PeekPending();

        if (timeout == 0 || jobs.Length > 0)
        {
            return jobs;
        }

        var newJob = await jobNotifier.ListenForNewJob((int)timeout, ct, jobTypes);

        return newJob is null ? jobs : await PeekPending();

        async Task<JobData[]> PeekPending()
        {
            return await db
                .JobDatas.FromSqlRaw(
                    PeekPendingJobsSql(Schema),
                    new NpgsqlParameter("now", Now()),
                    new NpgsqlParameter("completed_state", (int)JobState.Completed),
                    JobTypesParameter(jobTypes)
                )
                .AsNoTracking()
                .OrderBy(j => j.Id)
                .Take(MaxJobsLimit(maxJobs))
                .Select(j => new JobData(j.Id, j.JobType))
                .ToArrayAsync(ct);
        }
    }
    // Mostly Ai Generated - End

    // Mostly Ai Generated - Start
    public async Task<WaitForCompletedJobsResult?> WaitForCompletedJobsOrForSeconds(
        uint timeout,
        Instant after,
        uint? maxJobs,
        string[]? jobTypes,
        CancellationToken ct,
        Guid[]? jobIds = null
    )
    {
        if (jobIds is not null && jobIds.Length == 0)
        {
            return null;
        }

        var completed = await QueryCompleted();

        if (completed is not null)
        {
            return completed;
        }

        if (timeout != 0)
        {
            return await jobNotifier.ListenForCompletedJob((int)timeout, ct, jobTypes) is null
                ? null
                : await QueryCompleted();
        }

        while (true)
        {
            await jobNotifier.ListenForCompletedJob(Timeout.Infinite, ct, jobTypes);

            completed = await QueryCompleted();
            if (completed is not null)
            {
                return completed;
            }
        }

        async Task<WaitForCompletedJobsResult?> QueryCompleted()
        {
            var query = db
                .JobDatas.Where(j => j.CompletionTime > after)
                .Where(j => j.JobState == JobState.Completed);

            if (jobIds is not null)
            {
                query = query.Where(j => jobIds.Contains(j.Id));
            }

            if (jobTypes is not null)
            {
                query = query.Where(j => jobTypes.Contains(j.JobType));
            }

            var rows = await query
                .OrderBy(j => j.CompletionTime)
                .ThenBy(j => j.Id)
                .Take(MaxJobsLimit(maxJobs))
                .Select(j => new { j.Id, j.CompletionTime })
                .ToArrayAsync(ct);

            return rows.Length == 0
                ? null
                : new WaitForCompletedJobsResult(
                    [.. rows.Select(r => r.Id)],
                    rows[^1].CompletionTime!.Value
                );
        }
    }

    public async Task<bool> WaitPendingJobs(
        uint timeout,
        Guid? workerId,
        string[]? jobTypes,
        CancellationToken ct
    ) => (await PeekPendingJobs(timeout, 1, workerId, jobTypes, ct)).Length > 0;
    // Mostly Ai Generated - End
}

public record JobData(Guid JobId, string JobType);

public class JobNotifier
{
    private readonly NpgsqlConnection _conn;

    readonly string _schemaName;
    private Task listenRegisterer;

    string JobCompletedChannel => $"{_schemaName}_job_completed";
    string NewJobChannel => $"{_schemaName}_new_job";

    public JobNotifier(IConfiguration config, IHostApplicationLifetime host)
    {
        _conn = new NpgsqlConnection(config.GetConnectionString("PG"));

        _schemaName = PGContext.GetSchemaName(config);

        var ct = host.ApplicationStopping;

        _conn.Notification += (_, e) =>
        {
            if (
                e.Channel == NewJobChannel
                && e.Payload.TryDeserialize<JobData>() is { } newJobData
            )
                NewJobAddedEvent?.Invoke(newJobData);

            if (
                e.Channel == JobCompletedChannel
                && e.Payload.TryDeserialize<JobData>() is { } completedJobData
            )
                JobCompletedEvent?.Invoke(completedJobData);
        };

        listenRegisterer = Task.Run(
            async () =>
            {
                while (true)
                {
                    try
                    {
                        using var cmd = new NpgsqlCommand(
                            $"LISTEN {NewJobChannel}; LISTEN {JobCompletedChannel};",
                            _conn
                        );

                        if (_conn.State != System.Data.ConnectionState.Open)
                        {
                            await _conn.OpenAsync(ct);
                        }

                        await cmd.ExecuteNonQueryAsync(ct);
                        await _conn.WaitAsync(ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    }
                }
            },
            ct
        );
    }

    public delegate void JobEventHandler(JobData data);
    public event JobEventHandler? NewJobAddedEvent;
    public event JobEventHandler? JobCompletedEvent;

    public Task<JobData?> ListenForNewJob(
        int msTimeout,
        CancellationToken ct,
        string[]? jobTypes = null
    ) =>
        ListenFor(
            msTimeout,
            (handler) =>
            {
                NewJobAddedEvent += handler;
                return Disposable.Create(() => NewJobAddedEvent -= handler);
            },
            ct,
            jobTypes
        );

    public Task<JobData?> ListenForCompletedJob(
        int msTimeout,
        CancellationToken ct,
        string[]? jobTypes = null
    ) =>
        ListenFor(
            msTimeout,
            (handler) =>
            {
                JobCompletedEvent += handler;
                return Disposable.Create(() => JobCompletedEvent -= handler);
            },
            ct,
            jobTypes
        );

    private async Task<JobData?> ListenFor(
        int msTimeout,
        Func<JobEventHandler, IDisposable> sub,
        CancellationToken ct,
        string[]? jobTypes = null
    )
    {
        var semaphore = new SemaphoreSlim(0, 1);
        JobData? result = null;

        void EventHandler(JobData data)
        {
            if (
                (jobTypes is null || jobTypes.Contains(data.JobType))
                && semaphore.CurrentCount == 0
            )
            {
                result = data;
                semaphore.Release();
            }
        }
        using var eventHandle = sub(EventHandler);

        await await Task.WhenAny(semaphore.WaitAsync(ct), Task.Delay(msTimeout, ct));
        return result;
    }
}
