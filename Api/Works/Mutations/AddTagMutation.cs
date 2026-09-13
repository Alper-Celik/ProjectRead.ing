// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Utils;
using Api.Works.Queries;
using NodaTime;
using Riok.Mapperly.Abstractions;

namespace Api.Works.Mutations;

[MutationType]
public static partial class AddTagMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.TagWrite)]
    public static async Task<AddTagPayload> AddTagMutation(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        AddTagInput input,
        CancellationToken ct
    )
    {
        var tag = AddTagInputMapper.CreateFromDto(input, userId.Id!.Value, Now());

        await db.AddAsync(tag, cancellationToken: ct);
        await db.SaveChangesAsync(cancellationToken: ct);

        return new AddTagPayload(TagMapper.ToDto(tag));
    }
}

public record AddTagPayload(Queries.Tag Tag);

public record AddTagInput
{
    public required string[] TagNamespace { get; init; }

    public required string TagName { get; init; }
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

    [UserMapping(Default = true)]
    public static ZonedDateTime FromInstantToZonedDateTime(Instant i) =>
        MapperUtils.FromInstantToZonedDateTime(i);
}
// Mostly Ai Generated - End
