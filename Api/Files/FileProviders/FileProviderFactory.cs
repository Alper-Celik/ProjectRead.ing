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
            FileProviderId.LocalFsProvider => new LocalFSFileProvider(
                config["LocalFsPath"]
            ),
            _ => null,
        };
    }

    // public Task<IFileProvider> New(IServiceProvider services,FileKind kind)
}
