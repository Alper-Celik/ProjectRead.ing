// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Api.Database;
using Api.Files.Endpoints;
using Api.Files.FileProviders;
using Api.Files.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Files;

public class Setup
{
    // Mostly Ai Generated - Start
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<FileProviderFactory>();
        services.AddScoped<UserFileRouter>();
    }

    public static void MapEndpoints(IEndpointRouteBuilder route)
    {
        route.MapGet("/{id:guid}", GetFileEndpoint.GetFileAsync);
        route
            .MapPost("/{id:guid}", UploadFileEndpoint.UploadFileAsync)
            .DisableAntiforgery();
    }

    // Mostly Ai Generated - End

    public static async Task SeedDb(PGContext db, IConfiguration config)
    {
        var defaultConfigExists = await db
            .FileProviderBackendConfigs.Where(cfg =>
                cfg.InstanceWide == true && cfg.FallbackDefault == true
            )
            .AsNoTracking()
            .AnyAsync();
        if (!defaultConfigExists)
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
                FallbackDefault = true,
                DefaultFor = [],
                ProviderId = FileProviderId.LocalFsProvider,
                ProviderConfig = JsonSerializer.SerializeToDocument(
                    new LocalFSFileProviderConfig(
                        $"./BlobStorage{config["PR_TestPrefix"] ?? ""}"
                    )
                ),
            };

            await db.FileProviderBackendConfigs.AddAsync(fsCfg);
            await db.SaveChangesAsync();
        }
    }
}
