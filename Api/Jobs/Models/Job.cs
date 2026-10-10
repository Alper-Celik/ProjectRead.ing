// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using NodaTime;

namespace Api.Jobs.Models;

[Table("job_datas")]
public class JobData
{
    public required Guid Id { get; set; }

    public required string JobType { get; set; }
    public JsonDocument? JobArguments { get; set; }
    public JsonDocument? JobResult { get; set; }

    public JobState JobState { get; set; }
    public Instant? CompletionTime { get; set; }

    public Guid? LeaserId { get; set; }
    public Instant? LeaseEnd { get; set; }
    public Instant? LeasedAt { get; set; }
    public Duration LeaseLength { get; set; }

    public Guid[]? FailedLeasers { get; set; }

    public int MaxRetries { get; set; }
    public int CurrentTry { get; set; }

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
