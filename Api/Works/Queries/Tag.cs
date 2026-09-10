// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
using Api.Auth.Utils;
using Api.Database;
using Api.Works.Models;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Api.Works.Queries;

[QueryType]
public static partial class TagQuerry
{
    [PermissionCheckAuthorize(Auth.Models.UserPermissionBits.TagRead)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Tag>> GetTags(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        [Service] TagNode.ITagByIdDataLoader tagById,
        QueryContext<Tag> qc,
        PagingArguments pg,
        CancellationToken ct
    )
    {
        return await db
            .WorkTags.Where(wt => wt.OwnerId == userId.Id)
            .ProjectToDto()
            .With(qc)
            .ToPageWithDataLoaderAsync(pg, tagById, ct);
    }
}

[ObjectType<Tag>]
public static partial class TagNode
{
    public interface ITagByIdDataLoader : IBatchDataLoader<Guid, Tag>;

    public static async Task<IDictionary<Guid, Tag>> GetTagByIdAsync(
        IReadOnlyList<Guid> ids,
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        CancellationToken ct
    )
    {
        return await db
            .WorkTags.Where(wt => wt.OwnerId == userId.Id)
            .Where(wt => ids.Contains(wt.Id))
            .ProjectToDto()
            .ToDictionaryAsync(wt => wt.Id, ct);
    }
}

[Node]
public class Tag : IEntityMetadata, INode
{
    public static byte IdPostfix => WorkTag.IdPostfix;

    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    public required string[] TagNamespace { get; set; }

    public required string TagName { get; set; }
}

[Mapper]
public static partial class TagMapper
{
    [MapperIgnoreSource(nameof(WorkTag.WorkTagWorks))]
    [MapperIgnoreSource(nameof(WorkTag.Works))]
    [MapperIgnoreSource(nameof(WorkTag.Owner))]
    public static partial Tag ToDto(WorkTag w);

    public static partial IQueryable<Tag> ProjectToDto(this IQueryable<WorkTag> q);
}
