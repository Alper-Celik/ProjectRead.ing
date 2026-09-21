// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Buffers;
using System.Reactive.Disposables;
using System.Security.Cryptography;

namespace Api.Files.FileProviders;

public record LocalFSFileProviderConfig(string? BasePath);

public class LocalFSFileProvider(LocalFSFileProviderConfig config) : IFileProvider
{
    public async Task<Stream?> GetFileAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var path = System.IO.Path.Join(
            config.BasePath,
            ownerId.ToString(),
            id.ToString()
        );

        // in fuse or network filesystems checking and opening file might be blocking non trivial amount of time
        return await Task.Run(
            () => System.IO.Path.Exists(path) ? File.OpenRead(path) : null,
            ct
        );
    }

    public async Task<bool> SetFileAsync(
        Guid ownerId,
        Guid id,
        Stream stream,
        byte[] sha256Hash,
        long sizeBytes,
        CancellationToken ct
    )
    {
        var tempDir = System.IO.Path.Join(config.BasePath, "uploading");
        var tmpPath = System.IO.Path.Join(tempDir, Random.Shared.GetHexString(10, true));
        var storageDir = System.IO.Path.Join(config.BasePath, ownerId.ToString());
        var storagePath = System.IO.Path.Join(storageDir, id.ToString());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(storageDir);
        if (File.Exists(storagePath))
            return false;

        using var freeTempFile = Disposable.Create(() => File.Delete(tmpPath));
        using var handle = new FileStream(
            tmpPath,
            new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Options = FileOptions.Asynchronous,
                PreallocationSize = sizeBytes,
                UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite,
            }
        );

        // see https://github.com/dotnet/dotnet/blob/b0f34d51fccc69fd334253924abd8d6853fad7aa/src/runtime/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs#L126
        const int defaultCopyBufferSize = 81920;
        var copyBuffer = ArrayPool<byte>.Shared.Rent(defaultCopyBufferSize);
        using var freeBuffer = Disposable.Create(() =>
            ArrayPool<byte>.Shared.Return(copyBuffer)
        );
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        int bytesRead = 0,
            totalBytesRead = 0;
        while (
            (bytesRead = await stream.ReadAsync(new Memory<byte>(copyBuffer), ct)) != 0
        )
        {
            totalBytesRead += bytesRead;
            if (totalBytesRead > sizeBytes)
            {
                return false;
            }
            var data = new ReadOnlyMemory<byte>(copyBuffer, 0, bytesRead);
            hasher.AppendData(data.Span);
            await handle.WriteAsync(data, ct);
        }

        if (!hasher.GetHashAndReset().SequenceEqual(sha256Hash))
        {
            return false;
        }

        handle.Close();
        try
        {
            File.Move(tmpPath, storagePath);
        }
        catch (IOException)
        {
            return false;
        }

        return true;
    }
}
