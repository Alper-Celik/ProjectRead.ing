// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

namespace Api.Files.FileProviders;

public interface IFileProvider
{
    public Task<Stream?> GetFileAsync(Guid ownerId, Guid id, CancellationToken ct);

    public Task<Guid> CreateFileAsync(
        Guid ownerId,
        FileKind fileKind,
        string contentType,
        string originalFileName
    );

    public Task SetFileAsync(Guid ownerId, Guid id, Stream stream, CancellationToken ct);
}
