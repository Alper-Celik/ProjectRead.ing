// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

public abstract class FilesTestBase : WorksTestBase
{
    protected static readonly byte[] SampleContent = Encoding.UTF8.GetBytes(
        "ProjectRead.ing file api test payload 0123456789"
    );

    protected const string SampleContentType = "application/epub+zip";

    protected static string Sha256Hex(byte[] content) =>
        Convert.ToHexStringLower(SHA256.HashData(content));

    protected static AddFileRecordInput FileRecordInput(
        byte[]? content = null,
        string? fileName = "book.epub",
        FileKind fileKind = FileKind.UserFile,
        string contentType = SampleContentType,
        string? sha256 = null,
        long? sizeBytes = null
    )
    {
        var bytes = content ?? SampleContent;
        return new AddFileRecordInput
        {
            FileKind = fileKind,
            ContentType = contentType,
            OriginalFileName = fileName,
            SizeBytes = sizeBytes ?? bytes.LongLength,
            Sha256 = sha256 ?? Sha256Hex(bytes),
        };
    }

    protected static Upload UploadOf(byte[] content, string fileName = "book.epub") =>
        new(fileName, new MemoryStream(content));

    protected static async Task<Guid> AddFileRecordAsync(
        ApiClient client,
        AddFileRecordInput input
    )
    {
        var result = await client.Mutation(
            new { input },
            static (i, m) =>
                m.AddFileRecordMutation(i.input, p => p.FileRecord(f => f.Id))
        );
        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        return NodeIdToGuid(result.Data!);
    }

    protected async Task<(ApiClient Graphql, HttpClient Http)> AuthenticatedClients(
        string name = "files",
        string? password = null
    )
    {
        var graphqlHttp = Factory.CreateClient();
        graphqlHttp.BaseAddress = new Uri(
            graphqlHttp.BaseAddress!.AbsoluteUri + "graphql/"
        );
        var client = new ApiClient(graphqlHttp);

        var result = await Register(client, name, password ?? Password);
        result.HttpResponseMessage.EnsureSuccessStatusCode();
        await Assert.That(result.Errors).IsNull().Or.IsEmpty();

        var authorization = new AuthenticationHeaderValue(result.Data!);
        graphqlHttp.DefaultRequestHeaders.Authorization = authorization;

        var http = Factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = authorization;

        return (client, http);
    }
}
// Mostly Ai Generated - End
