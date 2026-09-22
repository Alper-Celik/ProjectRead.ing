// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;
using static Api.Files.Queries.FileRecordDataLoaders;

namespace Api.Files.Queries;

[QueryType]
public static partial class FileRecordQuery
{
    [PermissionCheckAuthorize(UserPermissionBits.FileRead)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<FileRecord>> GetFileRecords(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        [Service] IFileRecordByIdDataLoader fileRecordById,
        QueryContext<FileRecord> qc,
        PagingArguments pagingArguments,
        CancellationToken ct
    )
    {
        return await db
            .FileRecords.Where(f => f.OwnerId == userId.Id)
            .ProjectToDto()
            .WithQueryContext(qc)
            .ToPageWithDataLoaderAsync(pagingArguments, fileRecordById, ct);
    }
}

[ObjectType<FileRecord>]
public static partial class FileRecordNode
{
    [PermissionCheckAuthorize(UserPermissionBits.FileRead)]
    [GraphQLName("sha256")]
    public static string GetSha256([Parent] FileRecord fileRecord) =>
        Convert.ToHexStringLower(fileRecord.SHA256);

    [PermissionCheckAuthorize(UserPermissionBits.FileRead)]
    [GraphQLIgnore]
    public static async Task<FileRecord?> GetByIdAsync(
        [Service] IFileRecordByIdDataLoader fileRecordById,
        Guid id,
        CancellationToken ct
    ) => await fileRecordById.LoadAsync(id, cancellationToken: ct);
}

public static class FileRecordDataLoaders
{
    public interface IFileRecordByIdDataLoader : IBatchDataLoader<Guid, FileRecord>;

    [GraphQLIgnore]
    [DataLoader<IFileRecordByIdDataLoader>]
    public static async Task<IDictionary<Guid, FileRecord>> GetFileRecordByIdAsync(
        IReadOnlyList<Guid> ids,
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        CancellationToken ct
    )
    {
        return await db
            .FileRecords.Where(f => f.OwnerId == userId.Id)
            .Where(f => ids.Contains(f.Id))
            .ProjectToDto()
            .ToDictionaryAsync(f => f.Id, cancellationToken: ct);
    }
}

[Node(
    NodeResolverType = typeof(FileRecordNode),
    NodeResolver = nameof(FileRecordNode.GetByIdAsync)
)]
public class FileRecord : IEntityMetadata, INode
{
    public static byte IdPostfix => Models.FileRecord.IdPostfix;

    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    public FileKind FileKind { get; set; }
    public Guid FileProviderBackendConfigId { get; set; }

    public bool Uploaded { get; set; }

    public string? OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    [GraphQLIgnore]
    public required byte[] SHA256 { get; set; }
}

[Mapper]
public static partial class FileRecordMapper
{
    public static partial FileRecord? ToDto(Models.FileRecord? f);

    public static partial IQueryable<FileRecord> ProjectToDto(
        this IQueryable<Models.FileRecord> q
    );
}
// Mostly Ai Generated - End
