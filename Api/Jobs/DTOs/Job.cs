// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Api.Jobs.Models;
using NodaTime;

namespace Api.Jobs.DTOs;

public interface IJobType : IJob, ILeasedJob
{
    static abstract IJob CreateFrom(JobData data);
}

// TODO: re-add [InterfaceType("Job")] once the Jobs slice is wired — no object type implements
// this interface yet, so schema validation (and the generated ZeroQL client) cannot handle it.
public interface IJob : INode
{
    [ID]
    public Guid Id { get; set; }

    public JobState State { get; set; }

    public Guid? LatestLeaserId { get; set; }
    public Instant? LeaseEnd { get; set; }
    public Instant? LeasedAt { get; set; }

    static string JobType { get; }
    public JsonElement RawJobArguments { get; set; }

    public int MaxRetries { get; set; }
    public int TotalRetries { get; set; }

    public int MaxLeaseExtensions { get; set; }
    public int CurrentLeaseExtension { get; set; }

    public Guid[]? FailedLeasers { get; set; }

    public ILeasedJob AsLeased();
}

public interface ILeasedJob : IJob
{
    public new Guid LatestLeaserId { get; set; }
    public new Instant LeaseEnd { get; set; }
    public new Instant LeasedAt { get; set; }
}

public enum JobState
{
    New,
    HadLeased,
    CurrentlyLeased,
    Completed,
    FailedCompletion,
}
