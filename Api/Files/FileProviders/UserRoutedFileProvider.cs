using Api.Database;
using Api.Files.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Files.FileProviders;

public class UserRoutedFileProvider(PGContext db) : IFileProvider
{
    public async Task<Stream?> GetFileAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var fr = await db
            .FileRecords.Where(fr => fr.OwnerId == ownerId && fr.Id == id)
            .Where(fr =>
                fr.FileProviderBackend.OwnerId == ownerId
                || fr.FileProviderBackend.InstanceWide
            )
            .Select(fr => new
            {
                fr.FileProviderBackend.ProviderId,
                fr.FileProviderBackend.ProviderConfig,
                fr.FileProviderBackend.EncryptedSecrets,
            })
            .FirstOrDefaultAsync(ct);
        if (fr is null)
        {
            return null;
        }
    }

    public Task SetFileAsync(
        Guid ownerId,
        Guid id,
        FileKind kind,
        Stream stream,
        CancellationToken ct
    )
    {
        throw new NotImplementedException();
    }
}
