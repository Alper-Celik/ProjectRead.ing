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
using FairyBread;
using FluentValidation;
using NodaTime;
using Riok.Mapperly.Abstractions;

namespace Api.Works.Mutations;

[MutationType]
public static partial class AddAuthorMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.AuthorWrite)]
    public static async Task<AddAuthorPayload> AddAuthorMutation(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        [Service] IEFTransactionDIAccessorService txGetter,
        AddAuthorInput input,
        CancellationToken ct
    )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync();

        var author = AddAuthorInputMapper.CreateFromDto(input, userId.Id!.Value, Now());

        await db.AddAsync(author, cancellationToken: ct);
        await db.SaveChangesAsync(cancellationToken: ct);

        await tx.CommitAsync(ct);
        return new AddAuthorPayload(AuthorMapper.ToDto(author));
    }
}

public record AddAuthorPayload(Queries.Author Author);

public record AddAuthorInput
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public required string DisplayName { get; init; }

    public required List<string> PenNames { get; init; } = [];
}

[Mapper]
public static partial class AddAuthorInputMapper
{
    [MapperIgnoreTarget(nameof(Models.Author.RowVersion))]
    private static partial Models.Author CreateFromDtoInternal(
        AddAuthorInput w,
        Guid id,
        Instant metadataAddedAt,
        Instant metadataUpdatedAt
    );

    public static Models.Author CreateFromDto(AddAuthorInput w, Guid ownerId, Instant now)
    {
        var id = Guid.CreateVersion7().WithPostfix(Models.Author.IdPostfix);
        var author = CreateFromDtoInternal(w, id, now, now);
        author.OwnerId = ownerId;

        return author;
    }

    [UserMapping(Default = true)]
    public static ZonedDateTime FromInstantToZonedDateTime(Instant i) =>
        MapperUtils.FromInstantToZonedDateTime(i);
}
// Mostly Ai Generated - End
