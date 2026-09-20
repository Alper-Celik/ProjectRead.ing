// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

namespace Api.Files.FileProviders;

public class FileProviderFactory
{
    public async Task<IFileProvider?> New(
        IServiceProvider services,
        FileProviderId id,
        JsonDocument? providerConfig,
        List<byte[]>? encryptedSecrets
    )
    {
        var config = services.GetService<IConfiguration>();

        return id switch
        {
            FileProviderId.LocalFsProvider =>
                providerConfig?.TryDeserialize<LocalFSFileProviderConfig>() switch
                {
                    LocalFSFileProviderConfig fsCfg => new LocalFSFileProvider(fsCfg),
                    _ => null,
                },
            _ => null,
        };
    }

    // public Task<IFileProvider> New(IServiceProvider services,FileKind kind)
}
