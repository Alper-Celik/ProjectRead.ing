// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
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
using Npgsql;

namespace Api.Works.Mutations;

[MutationType]
public static partial class UpdateTagMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.TagRead | UserPermissionBits.TagWrite)]
    public static async Task<UpdateTagPayload> UpdateTagMutation(
        [Service] PGContext db,
        [Service] IEFTransactionDIAccessorService txGetter,
        UpdateTagInput input,
        CancellationToken ct
    )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync();
        var tag = await db.WorkTags.SingleAsync(
            t => t.Id == input.Id,
            cancellationToken: ct
        );

        UpdateTagInput.ApplyToTag(tag, input);
        tag.RowVersion += 1;
        tag.MetadataUpdatedAt = Now();

        await db.SaveChangesOrThrowAsync(
            PostgresErrorCodes.UniqueViolation,
            ErrorCodes.TAG_ALREADY_EXISTS,
            "A tag with the same namespace and name already exists",
            ct
        );

        await tx.CommitAsync(ct);
        return new UpdateTagPayload(TagMapper.ToDto(tag));
    }
}

public record UpdateTagPayload(Queries.Tag Tag);

public record UpdateTagInput : IBasicEntityMetadata
{
    public static byte IdPostfix => Models.WorkTag.IdPostfix;

    public Guid Id { get; init; }
    public int RowVersion { get; init; }

    public required Optional<string[]> TagNamespace { get; init; }
    public required Optional<string> TagName { get; init; }

    public class UpdateTagInputValidator : AbstractValidator<UpdateTagInput>
    {
        public UpdateTagInputValidator(
            PGContext db,
            ICurrentUserId userId,
            IEFTransactionDIAccessorService tx
        )
        {
            RuleFor(w => w).BeginTransaction(tx);

            RuleFor(w => w.Id).IdMustExist(db.WorkTags, userId.Id);

            RuleFor(w => w.RowVersion).RowVersionMustMatch(db.WorkTags);

            RuleFor(w => w)
                .MustAsync(
                    async (input, ct) =>
                    {
                        var existing = await db
                            .WorkTags.Where(t =>
                                t.OwnerId == userId.Id && t.Id == input.Id
                            )
                            .Select(t => new { t.TagNamespace, t.TagName })
                            .FirstOrDefaultAsync(ct);

                        if (existing is null)
                            return true;

                        var tagNamespace = input.TagNamespace.HasValue
                            ? input.TagNamespace.Value ?? []
                            : existing.TagNamespace;
                        var tagName = input.TagName.HasValue
                            ? input.TagName.Value
                            : existing.TagName;

                        return !await db.WorkTags.AnyAsync(
                            other =>
                                other.OwnerId == userId.Id
                                && other.Id != input.Id
                                && other.TagNamespace == tagNamespace
                                && other.TagName == tagName,
                            ct
                        );
                    }
                )
                .WithMessage("A tag with the same namespace and name already exists")
                .WithErrorCode(ErrorCodes.TAG_ALREADY_EXISTS);
        }
    }

    public static void ApplyToTag(Models.WorkTag tag, UpdateTagInput input)
    {
        if (input.TagNamespace.HasValue)
            tag.TagNamespace = input.TagNamespace.Value ?? [];

        if (input.TagName.HasValue)
            tag.TagName = input.TagName.Value;
    }
}
// Mostly Ai Generated - End
