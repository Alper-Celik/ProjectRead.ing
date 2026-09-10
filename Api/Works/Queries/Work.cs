// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;
using static Api.Works.Queries.AuthorDataLoaders;
using static Api.Works.Queries.WorkDataLoaders;

namespace Api.Works.Queries;

[QueryType]
public static partial class WorkQuery
{
    [PermissionCheckAuthorize(UserPermissionBits.WorkRead)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Work>> GetWorks(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        [Service] IWorkByIdDataLoader workById,
        QueryContext<Work> qc,
        PagingArguments pagingArguments,
        CancellationToken ct
    )
    {
        return await db
            .Works.Where(w => w.OwnerId == userId.Id)
            .ProjectToDto()
            .With(qc)
            .ToPageWithDataLoaderAsync(pagingArguments, workById, ct);
    }
}

[ObjectType<Work>]
public static partial class WorkNode
{
    [PermissionCheckAuthorize(
        UserPermissionBits.AuthorRead | UserPermissionBits.WorkRead
    )]
    [GraphQLName("Authors")]
    public static async Task<IReadOnlyList<Author>> GetAuthorsByWorkAsync(
        [Parent] Work work,
        IAuthorIdByWorkIdDataLoader authorIdLoader,
        IAuthorByIdDataLoader authorLoader,
        CancellationToken ct
    )
    {
        var ids = await authorIdLoader.LoadAsync(work.Id, ct);
        return (await authorLoader.LoadAsync(ids!, ct))!;
    }

    [PermissionCheckAuthorize(UserPermissionBits.AuthorRead)]
    [GraphQLIgnore]
    public static async Task<Work?> GetByIdAsync(
        [Service] IWorkByIdDataLoader workById,
        Guid id,
        CancellationToken ct
    ) => await workById.LoadAsync(id, cancellationToken: ct);
}

public static class WorkDataLoaders
{
    public interface IAuthorIdByWorkIdDataLoader : IBatchDataLoader<Guid, Guid[]>;

    [DataLoader<IAuthorIdByWorkIdDataLoader>]
    public static async Task<IDictionary<Guid, Guid[]>> GetAuthorIdByWorkIdAsync(
        IReadOnlyList<Guid> ids,
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        CancellationToken ct
    )
    {
        return await db
            .Works.Where(w => w.OwnerId == userId.Id)
            .Where(w => ids.Contains(w.Id))
            .Select(w => new
            {
                wId = w.Id,
                aIds = w.Work_Authors.Select(wa => wa.AuthorId),
            })
            .ToDictionaryAsync(t => t.wId, t => t.aIds.ToArray(), ct);
    }

    public interface IWorkByIdDataLoader : IBatchDataLoader<Guid, Work>;

    [DataLoader<IWorkByIdDataLoader>]
    public static async Task<IDictionary<Guid, Work>> GetWorkByIdAsync(
        IReadOnlyList<Guid> ids,
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        CancellationToken ct
    )
    {
        return await db
            .Works.Where(w => w.OwnerId == userId.Id)
            .Where(w => ids.Contains(w.Id))
            .ProjectToDto()
            .ToDictionaryAsync(w => w.Id, cancellationToken: ct);
    }
}

[Node(NodeResolverType = typeof(WorkNode), NodeResolver = nameof(WorkNode.GetByIdAsync))]
public class Work : IEntityMetadata, INode
{
    public static byte IdPostfix => Models.Work.IdPostfix;

    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public NodaTime.Instant? WorkPublishedAt { get; set; }
    public NodaTime.Instant? WorkUpdatedAt { get; set; }
    public List<WorkIdentifier> WorkIdentifiers { get; set; } = [];
}

public record WorkIdentifier(string WorkIdentifierType, string WorkIdentifierValue);

[Mapper]
public static partial class WorkMapper
{
    public static partial Models.WorkIdentifier FromWorkIdDto(WorkIdentifier w);

    public static partial Work? ToDto(Models.Work? w);

    public static partial IQueryable<Work> ProjectToDto(this IQueryable<Models.Work> q);
}
