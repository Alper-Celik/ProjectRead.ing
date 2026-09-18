namespace Api.Files.FileProviders;

public class LocalFSFileProvider(string basePath) : IFileProvider
{
    public async Task<Stream?> GetFileAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var path = System.IO.Path.Join(basePath, ownerId.ToString(), id.ToString());

        // in fuse or network filesystems checking and opening file might be blocking non trivial amount of time
        return await Task.Run(
            () => System.IO.Path.Exists(path) ? File.OpenRead(path) : null,
            ct
        );
    }

    public async Task SetFileAsync(
        Guid ownerId,
        Guid id,
        Stream stream,
        CancellationToken ct
    )
    {
        var dir = System.IO.Path.Join(basePath, ownerId.ToString());
        var path = System.IO.Path.Join(dir, id.ToString());
        using var handle = await Task.Run(
            () =>
            {
                Directory.CreateDirectory(dir);
                return File.Open(
                    path,
                    FileMode.OpenOrCreate,
                    FileAccess.Write,
                    FileShare.Read
                );
            },
            cancellationToken: ct
        );

        await stream.CopyToAsync(handle, ct);
    }
}
