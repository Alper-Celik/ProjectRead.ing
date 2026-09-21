// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Database;
using Api.Files.Models;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Guid> CreateFileAsync(
        Guid userId,
        FileKind fileKind,
        string contentType,
        string originalFileName,
        byte[] sha256Hash
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
            SHA256 = sha256Hash,
            FileProviderBackendConfigId = providerBackendConfigId,
        };
        await db.FileRecords.AddAsync(fileRecord);
        await db.SaveChangesAsync();
        return fileRecord.Id;
    }

    public async Task<Stream?> GetFileAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var fr = await db
            .FileRecords.Where(fr => fr.OwnerId == ownerId && fr.Id == id)
            .Where(fr =>
                fr.FileProviderBackendConfig.OwnerId == ownerId
                || fr.FileProviderBackendConfig.InstanceWide
            )
            .Select(fr => new
            {
                fr.FileProviderBackendConfig.ProviderId,
                fr.FileProviderBackendConfig.ProviderConfig,
                fr.FileProviderBackendConfig.EncryptedSecrets,
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

        return await fileProvider.GetFileAsync(ownerId, id, ct);
    }

    public async Task SetFileAsync(
        Guid ownerId,
        Guid id,
        Stream stream,
        CancellationToken ct
    )
    {
        var fr = await db
            .FileRecords.Where(fr => fr.OwnerId == ownerId && fr.Id == id)
            .Where(fr =>
                fr.FileProviderBackendConfig.OwnerId == ownerId
                || fr.FileProviderBackendConfig.InstanceWide
            )
            .Select(fr => new
            {
                fr.FileProviderBackendConfig.ProviderId,
                fr.FileProviderBackendConfig.ProviderConfig,
                fr.FileProviderBackendConfig.EncryptedSecrets,

                fr.SHA256,
            })
            .FirstOrDefaultAsync(ct);

        if (fr is null)
        {
            throw new ArgumentException("file doesn't exist");
        }
        var fileProvider =
            await fileProviderFactory.New(
                services,
                fr.ProviderId,
                fr.ProviderConfig,
                fr.EncryptedSecrets
            ) ?? throw new NullReferenceException();

        await fileProvider.SetFileAsync(ownerId, id, stream, fr.SHA256, ct);
    }
}
