// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using NodaTime;

namespace Api.Jobs.Models;

public class Job
{
    public required Guid Id { get; set; }

    public required string JobType { get; set; }
    public JsonDocument? JobData { get; set; }
    public JsonDocument? JobResult { get; set; }

    public JobState JobState { get; set; }
    public Instant? CompletionTime { get; set; }

    public Guid? LeaserId { get; set; }
    public Instant? LeaseEnd { get; set; }

    public Guid[]? FailedLeasers { get; set; }

    public int MaxRetries { get; set; }
    public int TotalRetries { get; set; }

    public int MaxLeaseExtensions { get; set; }
    public int CurrentLeaseExtension { get; set; }
}

public enum JobState
{
    New,
    HaveOrHadLease,
    Completed,
    FailedCompletion,
}
