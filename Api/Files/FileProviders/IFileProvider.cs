using System.Text.Json;

namespace Api.Files.FileProviders;

public interface IFileProvider
{
    public Task<Stream?> GetFileAsync(Guid ownerId, Guid id, CancellationToken ct);

    public Task SetFileAsync(
        Guid ownerId,
        Guid id,
        FileKind kind,
        Stream stream,
        CancellationToken ct
    );
}
