// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Api.Auth.Handlers;
using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using Api.Jobs.DTOs;
using Api.Utils;
using FluentValidation;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Jobs.Mutations;

public record LeasePendingJobsInput(string[] JobTypes, int MaxJobs);

public record LeasePendingJobsPayload(ILeasedJob[] Jobs);

[MutationType]
public static partial class LeaseJobMutations
{
    [Authorize(PermissionPolicyProvider.RemoteServicePolicyName)]
    // TODO: remove once the Jobs slice is wired: there is no IJobsInterfae implementation and no
    // object type implementing ILeasedJob yet, which breaks schema validation and the generated
    // ZeroQL client. Re-add the [InterfaceType] attributes and validate the input explicitly
    // (validator.ValidateOrThrowInputAsync) at that point.
    [GraphQLIgnore]
    public static async Task<LeasePendingJobsPayload> LeasePendingJobs(
        [Service] ICurrentServiceId idGetter,
        [Service] IEFTransactionDIAccessorService tx,
        [Service] IJobsInterfae jobsInterfae,
        [Service] JobDTOBuilder j,
        LeasePendingJobsInput input,
        CancellationToken ct
    )
    {
        await tx.BeginOrGetTransactionAsync();
        var jobDatas = await jobsInterfae.LeasePendingJobs(
            0,
            (uint)input.MaxJobs,
            idGetter.Id ?? throw new ArgumentNullException(nameof(idGetter)),
            input.JobTypes,
            ct
        );
        var jobs = jobDatas
            .Select(jd =>
                (
                    j.CreateFrom(jd.JobType, jd)
                    ?? throw new UnreachableException(
                        "There must not exist a job with a unregistered type"
                    )
                ).AsLeased()
            )
            .ToArray();

        await tx.SaveAndCommitTX(ct);

        return new LeasePendingJobsPayload(jobs);
    }
}

public class LeasePendingJobsInputValidator : AbstractValidator<LeasePendingJobsInput>
{
    public LeasePendingJobsInputValidator(
        PGContext db,
        IEFTransactionDIAccessorService tx,
        ICurrentServiceId idGetter
    )
    {
        RuleFor(i => i).BeginTransaction(tx);

        RuleFor(i => i.MaxJobs).GreaterThan(0).LessThan(128);

        RuleFor(i => i.JobTypes)
            .MustAsync(
                async (_, jobTypes, ctx, ct) =>
                    await db
                        .RemoteServices.Where(s => s.Id == idGetter.Id)
                        .Select(s => s.AllowedJobs)
                        .Select(allowedTypes =>
                            jobTypes.All(jt => allowedTypes.Contains(jt))
                        )
                        .FirstOrDefaultAsync(ct)
            );
    }
}
