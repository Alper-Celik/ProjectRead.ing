// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

namespace Api.Files.FileProviders;

public interface IFileProvider
{
    public Task<Stream?> GetFileAsync(Guid ownerId, Guid id, CancellationToken ct);

    public Task<bool> SetFileAsync(
        Guid ownerId,
        Guid id,
        Stream stream,
        byte[] sha256Hash,
        long sizeBytes,
        CancellationToken ct
    );
}
