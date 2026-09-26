// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Api.Jobs.Models;
using NodaTime;

namespace Api.Jobs;

public interface IJobsInterfae
{
    /// <summary>
    /// waits for new jobs in specified <paramref name="jobTypes"/> or all jobs if it is
    /// null
    ///
    /// waits at max <paramref name="maxWaitForSeconds"/> or indefinitely if <paramref name="maxWaitForSeconds"/> is 0
    ///</summary>
    public Task<Guid[]> WaitForNewJobsOrForSeconds(
        uint maxWaitForSeconds,
        string[]? jobTypes,
        CancellationToken ct
    );

    public Task<Job[]> PeekPendingJobs(
        Guid? leaserId,
        string[]? jobTypes,
        CancellationToken ct
    );

    /// <summary>
    /// gets and leases pending jobs if there is any.
    /// </summary>
    ///
    /// <returns>
    /// list of jobs leased in the id of <paramref name="leaserId"/>
    ///
    /// or empty list if there isn't any jobs pending
    /// </returns>
    public Task<Job[]> GetAndLeasePendingJobs(
        Guid? leaserId,
        string[]? jobTypes,
        CancellationToken ct
    );

    public Task<IPeekJobResult[]> PeekJobs(Guid[] jobIds);

    public Task<ILeaseJobResult[]> LeaseJobs(Guid[] hobIds, CancellationToken ct);

    public Task<IExtendLeaseResult[]> ExtendJobLeases(Guid[] jobIds);

    public Task<JobMarkAsCompleteResult[]> MarkJobsAsComplete(
        JobCompletionData[] jobCompletions
    );

    /// <summary>
    /// waits for completed jobs in specified <paramref name="jobTypes"/> or all jobs if it is
    /// null
    ///
    /// waits at max <paramref name="maxWaitForSeconds"/> or indefinitely if <paramref name="maxWaitForSeconds"/> is 0
    ///</summary>
    ///
    /// <param name="jobIds"> limits result to specified ids does not filter by ids if null</param>
    ///
    /// <returns> returns null when <paramref name="maxWaitForSeconds"/> is active and passes</returns>
    public Task<WaitForCompletedJobsResult?> WaitForCompletedJobsOrForSeconds(
        uint maxWaitForSeconds,
        string[]? jobTypes,
        Instant after,
        CancellationToken ct,
        Guid[]? jobIds = null
    );
}

public record WaitForCompletedJobsResult(Guid[] JobIds, Instant LatestCompletedJobTime);

public record JobCompletionData(Guid JobId, JsonDocument? CompletionData);

#region PeekJobResult
public interface IPeekJobResult;

public record Success(Job Job) : IPeekJobResult;

public record JobDoesNotExists : IPeekJobResult;

#endregion

#region LeaseJobResult
// TODO: switch to discriminated unions or closed interfaces
public interface ILeaseJobResult;

public partial record NewLeaseEndTime : ILeaseJobResult;

public record LeasingFailed(LeaseExtendFailReason Reason);

public enum LeaseFailReason
{
    CurrentlyLeased,
    AlreadyCompleted,
    JobDoesNotExist,
}

#endregion


#region IExtendLeaseResult

// TODO: switch to discriminated unions or closed interfaces
public interface IExtendLeaseResult;

public partial record NewLeaseEndTime(Instant LeaseEnd) : IExtendLeaseResult;

public record LeaseExtendFailed(LeaseExtendFailReason Reason) : IExtendLeaseResult;

public enum LeaseExtendFailReason
{
    NotLeasedCurrently,
    LeasedBySomeoneElse,
    LeaseExtensionLimitReached,
    JobDoesNotExist,
}
#endregion

public enum JobMarkAsCompleteResult
{
    Completed,
    LeasedBySomeoneElse,
    NotLeasedCurrently,
    AlreadyCompleted,
    JobDoesNotExist,
}

public enum JobStatus
{
    Free,
    Leased,
    Completed,
    MaxRetryReached,
}
