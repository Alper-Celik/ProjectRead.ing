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

namespace Api.Jobs;

public class JobsInterfae(
    PGContext db,
    JobNotifier jobNotifier,
    IEFTransactionDIAccessorService tx,
    ICurrentServiceId serviceId
) : IJobsInterfae
{
    public async Task<Dictionary<Guid, IExtendLeaseResult>> ExtendJobLeases(
        Guid[] jobIds,
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

            if (job.LeaserId != serviceId.Id)
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

            job.LeaserId = serviceId.Id;
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
        Guid? leaserId,
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

            var result = await db
                .JobDatas.FromSqlRaw(
                    """
                    UPDATE job_datas j
                    SET leased_at = @now, 
                        lease_end = @now + lease_length,
                        leaser_id = @leaser_id,
                        job_state = @leased_state,
                        current_try = current_try + 1
                    WHERE j.id in (
                        SELECT id FROM job_datas
                        WHERE (cardinality(@job_types) = 0 OR job_type = ANY(@job_types))
                            AND (lease_end IS NULL OR lease_end < @now )
                            AND (NOT job_state = @completed_state)
                            AND (current_try <= max_retries)
                        ORDER BY id
                        LIMIT @max_jobs
                        FOR UPDATE SKIP LOCKED
                        )
                    RETURNING j.*

                    """,
                    new NpgsqlParameter("now", now),
                    new NpgsqlParameter("leaser_id", leaserId),
                    new NpgsqlParameter("leased_state", Models.JobState.HaveOrHadLease),
                    new NpgsqlParameter("completed_state", Models.JobState.Completed),
                    new NpgsqlParameter("job_types", jobTypes ?? []),
                    new NpgsqlParameter("max_jobs", maxJobs ?? int.MaxValue)
                )
                .Select(j => new JobData(j.Id, j.JobType))
                .ToArrayAsync(ct);
            return result;
        }

        if (timeout != 0)
        {
            jobs = await LeasePendingJobs(0, maxJobs, leaserId, jobTypes, ct);
        }

        if (jobs.Length > 0)
        {
            return jobs;
        }

        var newJob = await jobNotifier.ListenForNewJob((int)timeout, ct, jobTypes);

        if (newJob != null)
        {
            return await LeasePendingJobs(0, maxJobs, leaserId, jobTypes, ct);
        }

        return jobs;
    }

    public Task<Dictionary<Guid, JobMarkAsCompleteResult>> MarkJobsAsComplete(
        JobCompletionData[] jobCompletions,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<Guid, IPeekJobResult>> PeekJobs(
        Guid[] jobIds,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }

    public Task<JobData[]> PeekPendingJobs(
        uint timeout,
        uint? maxJobs,
        Guid? leaserId,
        string[]? jobTypes,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }

    public Task<WaitForCompletedJobsResult?> WaitForCompletedJobsOrForSeconds(
        uint timeout,
        Instant after,
        uint? maxJobs,
        string[]? jobTypes,
        CancellationToken ct,
        Guid[]? jobIds = null
    )
    {
        throw new NotImplementedException();
    }

    public Task<bool> WaitPendingJobs(
        uint timeout,
        Guid? leaserId,
        string[]? jobTypes,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }
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
