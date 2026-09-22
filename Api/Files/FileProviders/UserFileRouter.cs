// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Database;
using Api.Files.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Api.Files.FileProviders;

public class UserFileRouter(
    IServiceProvider services,
    PGContext db,
    FileProviderFactory fileProviderFactory
)
{
    public async Task<Guid> GetUsersPrefferedStorage(Guid userId, FileKind fileKind)
    {
        return await db
            .FileProviderBackendConfigs.AsNoTracking()
            .Where(cfg => cfg.InstanceWide == true || cfg.OwnerId == userId)
            .Where(cfg =>
                cfg.DefaultFor.Contains(fileKind) || cfg.FallbackDefault == true
            )
            .OrderBy(cfg => cfg.InstanceWide)
            .ThenBy(cfg => cfg.DefaultFor.Length)
            .ThenByDescending(cfg => cfg.Id)
            .Select(cfg => cfg.Id)
            .FirstAsync();
    }

    public async Task<FileRecord> CreateFileAsync(
        Guid userId,
        FileKind fileKind,
        string contentType,
        // Mostly Ai Generated - Start
        string? originalFileName,
        long sizeBytes,
        // Mostly Ai Generated - End
        byte[] sha256Hash,
        // Mostly Ai Generated - Start
        CancellationToken ct
    // Mostly Ai Generated - End
    )
    {
        Guid providerBackendConfigId = await GetUsersPrefferedStorage(userId, fileKind);
        var now = Now();
        var fileRecord = new FileRecord
        {
            Id = Guid.CreateVersion7().WithPostfix(FileRecord.IdPostfix),
            MetadataAddedAt = now,
            MetadataUpdatedAt = now,

            OwnerId = userId,
            FileKind = fileKind,
            ContentType = contentType,
            OriginalFileName = originalFileName,
            // Mostly Ai Generated - Start
            SizeBytes = sizeBytes,
            // Mostly Ai Generated - End
            SHA256 = sha256Hash,
            FileProviderBackendConfigId = providerBackendConfigId,
        };
        // Mostly Ai Generated - Start
        await db.FileRecords.AddAsync(fileRecord, ct);
        await db.SaveChangesAsync(ct);
        return fileRecord;
        // Mostly Ai Generated - End
    }

    // Mostly Ai Generated - Start
    public async Task<FileContent?> GetFileAsync(
        Guid ownerId,
        Guid id,
        CancellationToken ct
    )
    {
        var fr = await db
            .FileRecords.Where(fr => fr.OwnerId == ownerId && fr.Id == id && fr.Uploaded)
            .Where(fr =>
                fr.FileProviderBackendConfig.OwnerId == ownerId
                || fr.FileProviderBackendConfig.InstanceWide
            )
            .Select(fr => new
            {
                fr.FileProviderBackendConfig.ProviderId,
                fr.FileProviderBackendConfig.ProviderConfig,
                fr.FileProviderBackendConfig.EncryptedSecrets,

                fr.ContentType,
                fr.SHA256,
                fr.OriginalFileName,
                fr.MetadataAddedAt,
            })
            .FirstOrDefaultAsync(ct);

        if (fr is null)
        {
            return null;
        }

        var fileProvider = await fileProviderFactory.New(
            services,
            fr.ProviderId,
            fr.ProviderConfig,
            fr.EncryptedSecrets
        );

        if (fileProvider is null)
        {
            return null;
        }

        var stream = await fileProvider.GetFileAsync(ownerId, id, ct);
        if (stream is null)
        {
            return null;
        }
        return new FileContent(
            stream,
            fr.ContentType,
            fr.OriginalFileName,
            fr.SHA256,
            fr.MetadataAddedAt
        );
    }

    // Mostly Ai Generated - End

    // Mostly Ai Generated - Start
    /// <summary>
    /// Stores the content for an existing record of <paramref name="ownerId"/>.
    /// Returns the updated record, or <see langword="null"/> when the provider rejected the
    /// content because it did not match the declared size or SHA256. Throws when the record
    /// does not exist for the owner or the provider cannot be constructed.
    /// </summary>
    public async Task<FileRecord?> SetFileAsync(
        Guid ownerId,
        Guid id,
        Stream stream,
        CancellationToken ct
    )
    {
        var fr =
            await db
                .FileRecords.Include(fr => fr.FileProviderBackendConfig)
                .FirstOrDefaultAsync(
                    fr =>
                        fr.OwnerId == ownerId
                        && fr.Id == id
                        && (
                            fr.FileProviderBackendConfig.OwnerId == ownerId
                            || fr.FileProviderBackendConfig.InstanceWide
                        ),
                    ct
                )
            ?? throw new ArgumentException("file doesn't exist");

        var fileProvider =
            await fileProviderFactory.New(
                services,
                fr.FileProviderBackendConfig.ProviderId,
                fr.FileProviderBackendConfig.ProviderConfig,
                fr.FileProviderBackendConfig.EncryptedSecrets
            ) ?? throw new NullReferenceException();

        if (
            !await fileProvider.SetFileAsync(
                ownerId,
                id,
                stream,
                fr.SHA256,
                fr.SizeBytes,
                ct
            )
        )
        {
            return null;
        }

        fr.Uploaded = true;
        fr.RowVersion += 1;
        fr.MetadataUpdatedAt = Now();
        await db.SaveChangesAsync(ct);
        return fr;
    }
    // Mostly Ai Generated - End
}

// Mostly Ai Generated - Start
public record FileContent(
    Stream Stream,
    string ContentType,
    string? OriginalFileName,
    byte[] SHA256,
    Instant MetadataAddedAt
);
// Mostly Ai Generated - End
