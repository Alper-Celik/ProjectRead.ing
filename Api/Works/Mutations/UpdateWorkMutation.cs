// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later


using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using Api.Utils;
using Api.Works.Models;
using Api.Works.Queries;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using NodaTime;

namespace Api.Works.Mutations;

[MutationType]
public static partial class UpdateWorkMutations
{
    [PermissionCheckAuthorize(
            UserPermissionBits.WorkRead
            | UserPermissionBits.WorkWrite)]
    public static async Task<UpdateWorkPayload> UpdateWorkMutation(
            [Service] PGContext db,
            [Service] IEFTransactionDIAccessorService txGetter,
            CancellationToken ct,
            UpdateWorkInput input
            )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync();
        var work = await db.Works
            .Include(w => w.WorkTag_Works)
            .Include(w => w.Work_Authors)
            .SingleAsync(w => w.Id == input.Id, cancellationToken: ct);

        UpdateWorkInput.ApplyToWork(work, input);
        work.RowVersion += 1;
        work.MetadataAddedAt = Now();

        await db.SaveChangesAsync(cancellationToken: ct);

        await tx.CommitAsync();
        return new UpdateWorkPayload(WorkMapper.ToDto(work));
    }
}

public record UpdateWorkPayload(
        Queries.Work Work
        );

public record UpdateWorkInput : IBasicEntityMetadata
{
    public static byte IdPostfix => Models.Work.IdPostfix;

    public Guid Id { get; init; }
    public int RowVersion { get; init; }

    [DefaultValue("")]
    public required Optional<string> Title { get; init; }
    public Optional<string?> Description { get; init; }

    public Optional<Instant?> WorkPublishedAt { get; init; }
    public Optional<Instant?> WorkUpdatedAt { get; init; }
    public Optional<List<Queries.Work.WorkIdentifier>?> WorkIdentifiers { get; init; }
    public required Optional<List<Guid>?> TagIds { get; init; }
    public required Optional<List<Guid>?> AuthorIds { get; init; }

    public class UpdateWorkInputValidator : AbstractValidator<UpdateWorkInput>
    {
        public UpdateWorkInputValidator(PGContext db, ICurrentUserId userId, IEFTransactionDIAccessorService tx)
        {
            RuleFor(w => w).MustAsync(async (_, ct) => { await tx.BeginOrGetTransactionAsync(); return true; });

            RuleFor(w => w.Id).IdMustExist(db.Works, userId.Id);

            RuleFor(w => w.RowVersion).RowVersionMustMatch(db.Works);

            RuleFor(w => w.TagIds.Value!).MustBeDistinct(nameof(TagIds)).WhenOptionalSet(w => w.TagIds);
            RuleFor(w => w.TagIds.Value!).IdsMustExist(db.WorkTags, userId.Id).WhenOptionalSet(w => w.TagIds);


            RuleFor(w => w.AuthorIds.Value!).MustBeDistinct(nameof(AuthorIds)).WhenOptionalSet(w => w.AuthorIds);
            RuleFor(w => w.AuthorIds.Value!).IdsMustExist(db.Authors, userId.Id).WhenOptionalSet(w => w.AuthorIds);
        }
    }

    public static void ApplyToWork(Models.Work work, UpdateWorkInput input)
    {
        if (input.Title.HasValue)
            work.Title = input.Title.Value;

        if (input.Description.HasValue)
            work.Description = input.Description.Value;

        if (input.WorkPublishedAt.HasValue)
            work.WorkPublishedAt = input.WorkPublishedAt.Value?.InUtc();

        if (input.WorkUpdatedAt.HasValue)
            work.WorkUpdatedAt = input.WorkUpdatedAt.Value?.InUtc();

        if (input.WorkIdentifiers.HasValue)
            work.WorkIdentifiers = input.WorkIdentifiers.Value?.Select(WorkMapper.FromWorkIdDto).ToList() ?? [];


        if (input.TagIds.HasValue)
            work.WorkTag_Works = input.TagIds.Value?.Select(t => new WorkTag_Work(work.Id, t)).ToList() ?? [];

        if (input.AuthorIds.HasValue)
            work.Work_Authors = input.AuthorIds.Value?.Select(t => new Work_Author(work.Id, t)).ToList() ?? [];
    }

}