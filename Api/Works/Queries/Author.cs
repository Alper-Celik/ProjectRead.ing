// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
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
public static partial class AuthorQuerry
{
    [PermissionCheckAuthorize(Auth.Models.UserPermissionBits.AuthorRead)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Author>> GetAuthors(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        IAuthorByIdDataLoader authorById,
        QueryContext<Author> qc,
        PagingArguments pg,
        CancellationToken ct
    ) =>
        await db
            .Authors.Where(a => a.OwnerId == userId.Id)
            .ProjectToDto()
            .With(qc)
            .ToPageWithDataLoaderAsync(pg, authorById, ct);
}

[ObjectType<Author>]
public static partial class AuthorNode
{
    [PermissionCheckAuthorize(Auth.Models.UserPermissionBits.WorkRead)]
    [UseFiltering]
    [UseSorting]
    [GraphQLName("Works")]
    public static async Task<PageConnection<Work>> GetWorksByAuthor(
        [Parent] Author author,
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
            .Where(w => w.Authors.Select(a => a.Id).Contains(author.Id))
            .ProjectToDto()
            .With(qc)
            .ToPageWithDataLoaderAsync(pagingArguments, workById, ct);
    }

    [GraphQLIgnore]
    public static async Task<Author?> GetByIdAsync(
        IAuthorByIdDataLoader authorById,
        Guid id,
        CancellationToken ct
    ) => await authorById.LoadAsync(id, ct);
}

public static class AuthorDataLoaders
{
    public interface IAuthorByIdDataLoader : IBatchDataLoader<Guid, Author>;

    [DataLoader<IAuthorByIdDataLoader>]
    public static async Task<IDictionary<Guid, Author>> GetAuthorByIdAsync(
        IReadOnlyList<Guid> ids,
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        CancellationToken ct
    )
    {
        return await db
            .Authors.Where(a => a.OwnerId == userId.Id)
            .Where(a => ids.Contains(a.Id))
            .ProjectToDto()
            .ToDictionaryAsync(a => a.Id, cancellationToken: ct);
    }
}

[Node(
    NodeResolverType = typeof(AuthorNode),
    NodeResolver = nameof(AuthorNode.GetByIdAsync)
)]
public class Author : IEntityMetadata, INode
{
    public static byte IdPostfix => Models.Author.IdPostfix;

    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public required string DisplayName { get; set; }

    public required List<string> PenNames { get; set; }
}

[Mapper]
public static partial class AuthorMapper
{
    public static partial IQueryable<Author> ProjectToDto(
        this IQueryable<Models.Author> q
    );

    public static partial Author ToDto(Models.Author a);
}
