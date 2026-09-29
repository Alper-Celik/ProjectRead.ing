// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reactive.Disposables;
using System.Text.Json;
using Api.Database;
using Api.Jobs.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Npgsql;

namespace Api.Jobs;

public class JobsInterfae : IJobsInterfae
{
    public Task<IExtendLeaseResult[]> ExtendJobLeases(Guid[] jobIds)
    {
        throw new NotImplementedException();
    }

    public Task<ILeaseJobResult[]> LeaseJobs(Guid[] jobIds, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<JobData[]> LeasePendingJobs(
        uint timeout,
        uint? maxJobs,
        Guid? leaserId,
        string[]? jobTypes,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }

    public Task<JobMarkAsCompleteResult[]> MarkJobsAsComplete(
        JobCompletionData[] jobCompletions
    )
    {
        throw new NotImplementedException();
    }

    public Task<IPeekJobResult[]> PeekJobs(Guid[] jobIds)
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

class JobNotifier
{
    private readonly NpgsqlConnection _conn;

    public JobNotifier(PGContext db)
    {
        _conn = (NpgsqlConnection)db.Database.GetDbConnection();
        _conn.Notification += (_, e) =>
        {
            if (
                e.Payload == $"{db.SchemaName}_new_job"
                && e.Payload.TryDeserialize<JobData>() is { } newJobData
            )
                NewJobAddedEvent?.Invoke(newJobData);

            if (
                e.Payload == $"{db.SchemaName}_job_completed"
                && e.Payload.TryDeserialize<JobData>() is { } completedJobData
            )
                NewJobAddedEvent?.Invoke(completedJobData);
        };
    }

    public delegate void JobEventHandler(JobData data);
    public event JobEventHandler? NewJobAddedEvent;
    public event JobEventHandler? JobCompletedEvent;

    public Task<JobData?> ListenForNewJob(int msTimeout, CancellationToken ct) =>
        ListenFor(
            msTimeout,
            (handler) =>
            {
                NewJobAddedEvent += handler;
                return Disposable.Create(() => NewJobAddedEvent -= handler);
            },
            ct
        );

    public Task<JobData?> ListenForCompletedJob(int msTimeout, CancellationToken ct) =>
        ListenFor(
            msTimeout,
            (handler) =>
            {
                JobCompletedEvent += handler;
                return Disposable.Create(() => JobCompletedEvent -= handler);
            },
            ct
        );

    private async Task<JobData?> ListenFor(
        int msTimeout,
        Func<JobEventHandler, IDisposable> sub,
        CancellationToken ct
    )
    {
        Task connTask =
            _conn.State == System.Data.ConnectionState.Closed
                ? _conn.OpenAsync(ct)
                : Task.CompletedTask;

        var semaphore = new SemaphoreSlim(0, 1);
        JobData? result = null;

        void EventHandler(JobData data)
        {
            result = data;
            semaphore.Release();
        }
        using var eventHandle = sub(EventHandler);

        await connTask;
        await await Task.WhenAny(semaphore.WaitAsync(ct), Task.Delay(msTimeout, ct));
        return result;
    }
}
