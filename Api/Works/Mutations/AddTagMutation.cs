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
using Api.Works.Queries;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Npgsql;
using Riok.Mapperly.Abstractions;

namespace Api.Works.Mutations;

[MutationType]
public static partial class AddTagMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.TagWrite)]
    [Error(typeof(DomainError))]
    public static async Task<AddTagPayload> AddTagAsync(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        [Service] IEFTransactionDIAccessorService txGetter,
        [Service] IValidator<AddTagInput> validator,
        AddTagInput input,
        CancellationToken ct
    )
    {
        await validator.ValidateOrThrowInputAsync(input, ct);

        var tx = await txGetter.BeginOrGetTransactionAsync();

        var tag = AddTagInputMapper.CreateFromDto(input, userId.Id!.Value, Now());

        await db.AddAsync(tag, cancellationToken: ct);
        await db.SaveChangesOrThrowAsync(
            PostgresErrorCodes.UniqueViolation,
            ErrorCodes.TAG_ALREADY_EXISTS,
            "A tag with the same namespace and name already exists",
            ct
        );

        await tx.CommitAsync(ct);
        return new AddTagPayload(TagMapper.ToDto(tag));
    }
}

public record AddTagPayload(Queries.Tag Tag);

public record AddTagInput
{
    public required string[] TagNamespace { get; init; }

    public required string TagName { get; init; }

    public class AddTagInputValidator : AbstractValidator<AddTagInput>
    {
        public AddTagInputValidator(
            PGContext db,
            ICurrentUserId userId,
            IEFTransactionDIAccessorService tx
        )
        {
            RuleFor(t => t).BeginTransaction(tx);

            RuleForEach(t => t.TagNamespace)
                .Must(GeneralUtils.IsValidTagComponent)
                .WithMessage(
                    "Tag namespace components cannot be empty, contain the '::' separator, or start/end with ':'"
                )
                .WithErrorCode(ErrorCodes.TAG_FORBIDDEN_SEPARATOR);

            RuleFor(t => t.TagName)
                .Must(GeneralUtils.IsValidTagComponent)
                .WithMessage(
                    "Tag name cannot be empty, contain the '::' separator, or start/end with ':'"
                )
                .WithErrorCode(ErrorCodes.TAG_FORBIDDEN_SEPARATOR);

            RuleFor(t => t)
                .MustAsync(
                    async (input, ct) =>
                        !await db.WorkTags.AnyAsync(
                            existing =>
                                existing.OwnerId == userId.Id
                                && existing.TagNamespace == input.TagNamespace
                                && existing.TagName == input.TagName,
                            ct
                        )
                )
                .WithMessage("A tag with the same namespace and name already exists")
                .WithErrorCode(ErrorCodes.TAG_ALREADY_EXISTS);
        }
    }
}

[Mapper]
public static partial class AddTagInputMapper
{
    [MapperIgnoreTarget(nameof(Models.WorkTag.RowVersion))]
    [MapperIgnoreTarget(nameof(Models.WorkTag.WorkTagWorks))]
    [MapperIgnoreTarget(nameof(Models.WorkTag.Works))]
    [MapperIgnoreTarget(nameof(Models.WorkTag.Owner))]
    private static partial Models.WorkTag CreateFromDtoInternal(
        AddTagInput w,
        Guid id,
        Instant metadataAddedAt,
        Instant metadataUpdatedAt
    );

    public static Models.WorkTag CreateFromDto(AddTagInput w, Guid ownerId, Instant now)
    {
        var id = Guid.CreateVersion7().WithPostfix(Models.WorkTag.IdPostfix);
        var tag = CreateFromDtoInternal(w, id, now, now);
        tag.OwnerId = ownerId;

        return tag;
    }
}
// Mostly Ai Generated - End
