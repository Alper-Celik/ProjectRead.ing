// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Api.Database;
using Api.Files.FileProviders;
using Api.Files.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Files;

public class Setup
{
    public static void RegisterServices(IServiceCollection services) { }

    public static async Task SeedDb(PGContext db)
    {
        var defaultConfigExists = await db
            .FileProviderBackendConfigs.Where(cfg =>
                cfg.InstanceWide == true && cfg.FallbackDefault == true
            )
            .AsNoTracking()
            .AnyAsync();
        if (defaultConfigExists)
        {
            var now = Now();
            var fsCfg = new FileProviderBackendConfig()
            {
                Id = Guid.CreateVersion7()
                    .WithPostfix(FileProviderBackendConfig.IdPostfix),
                MetadataAddedAt = now,
                MetadataUpdatedAt = now,
                OwnerId = null,

                InstanceWide = true,
                ProviderId = FileProviderId.LocalFsProvider,
                ProviderConfig = JsonSerializer.SerializeToDocument(
                    new LocalFSFileProviderConfig("./BlobStorage")
                ),
            };

            await db.FileProviderBackendConfigs.AddAsync(fsCfg);
            await db.SaveChangesAsync();
        }
    }
}
