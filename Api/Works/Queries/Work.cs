// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
using Api.Auth.Utils;
using Api.Database;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Riok.Mapperly.Abstractions;

namespace Api.Works.Queries;

[QueryType]
public static partial class WorkQuery
{
    [PermissionCheckAuthorize(Auth.Models.UserPermissionBits.WorkRead)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Work>> GetWorks(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        QueryContext<Work> qc,
        PagingArguments pagingArguments,
        CancellationToken ct
    )
    {
        return await db
            .Works.Where(w => w.OwnerId == userId.Id)
            .ProjectToDto()
            .With(qc)
            .ToPageAsync(pagingArguments, cancellationToken: ct);
    }
}

[Node]
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

    public record WorkIdentifier(string WorkIdentifierType, string WorkIdentifierValue);

    public static async Task<Work?> GetAsync(
        [Service] PGContext db,
        Guid id,
        CancellationToken ct
    ) => WorkMapper.ToDto(await db.Works.FindAsync([id], cancellationToken: ct));
}

[Mapper]
public static partial class WorkMapper
{
    public static partial Models.WorkIdentifier FromWorkIdDto(Work.WorkIdentifier w);

    public static partial Work? ToDto(Models.Work? w);

    public static partial IQueryable<Work> ProjectToDto(this IQueryable<Models.Work> q);
}
