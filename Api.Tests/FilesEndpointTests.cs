// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Api.Auth.Models;
using Api.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

public class FilesEndpointTests : FilesTestBase
{
    [Test]
    public async Task GetFile_WithoutToken_ReturnsUnauthorized(CancellationToken ct)
    {
        var http = Factory.CreateClient();

        using var get = await http.GetAsync(
            $"api/files/{Guid.CreateVersion7().WithPostfix(0xFF)}",
            ct
        );
        using var post = await http.PostAsync(
            $"api/files/{Guid.CreateVersion7().WithPostfix(0xFF)}",
            new MultipartFormDataContent(),
            ct
        );

        await Assert.That(get.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(post.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetFile_WithReadOnlyPermissions_ReturnsForbiddenOnUpload(
        CancellationToken ct
    )
    {
        var (client, http) = await AuthenticatedClients("readonly");
        var id = await AddFileRecordAsync(client, FileRecordInput());

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PGContext>();
            await db.UserTokens.ExecuteUpdateAsync(
                t => t.SetProperty(x => x.Permissions, UserPermissionBits.FileRead),
                ct
            );
        }

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(SampleContent), "file", "book.epub");

        using var read = await http.GetAsync($"api/files/{id}", ct);
        using var upload = await http.PostAsync($"api/files/{id}", form, ct);

        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(upload.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UploadFile_StoresContentAndMarksItUploaded(CancellationToken ct)
    {
        var (client, http) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(client, FileRecordInput());

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(SampleContent);
        file.Headers.ContentType = new MediaTypeHeaderValue(SampleContentType);
        form.Add(file, "file", "book.epub");

        using var upload = await http.PostAsync($"api/files/{id}", form, ct);

        await Assert.That(upload.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        using var read = await http.GetAsync($"api/files/{id}", ct);

        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert
            .That(read.Content.Headers.ContentType!.MediaType)
            .IsEqualTo(SampleContentType);
        await Assert.That(read.Headers.AcceptRanges.Contains("bytes")).IsTrue();
        await Assert.That(read.Headers.CacheControl!.Private).IsTrue();
        await Assert
            .That(read.Headers.CacheControl.MaxAge)
            .IsEqualTo(TimeSpan.FromSeconds(31536000));
        await Assert
            .That(read.Headers.CacheControl.Extensions.Any(e => e.Name == "immutable"))
            .IsTrue();
        await Assert
            .That(read.Headers.TryGetValues("X-Content-Type-Options", out var nosniff))
            .IsTrue();
        await Assert.That(nosniff!.Single()).IsEqualTo("nosniff");
        await Assert
            .That(await read.Content.ReadAsByteArrayAsync(ct))
            .IsEquivalentTo(SampleContent);
    }

    [Test]
    public async Task GetFile_WithRangeRequest_ReturnsPartialContent(CancellationToken ct)
    {
        var (client, http) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(client, FileRecordInput());

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(SampleContent), "file", "book.epub");
        using var upload = await http.PostAsync($"api/files/{id}", form, ct);
        await Assert.That(upload.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/files/{id}");
        request.Headers.Range = new RangeHeaderValue(1, 3);
        using var response = await http.SendAsync(request, ct);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.PartialContent);
        await Assert
            .That(response.Content.Headers.ContentRange!.ToString())
            .IsEqualTo($"bytes 1-3/{SampleContent.Length}");
        await Assert
            .That(await response.Content.ReadAsByteArrayAsync(ct))
            .IsEquivalentTo(SampleContent[1..4]);
    }

    [Test]
    public async Task GetFile_WithUnsafeContentType_ServesAttachmentAsOctetStream(
        CancellationToken ct
    )
    {
        var (client, http) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(
            client,
            FileRecordInput(
                content: SampleContent,
                fileName: "page.html",
                contentType: "text/html"
            )
        );

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(SampleContent), "file", "page.html");
        using var upload = await http.PostAsync($"api/files/{id}", form, ct);
        await Assert.That(upload.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        using var read = await http.GetAsync($"api/files/{id}", ct);

        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert
            .That(read.Content.Headers.ContentType!.MediaType)
            .IsEqualTo("application/octet-stream");
        await Assert
            .That(read.Content.Headers.ContentDisposition!.DispositionType)
            .IsEqualTo("attachment");
        await Assert
            .That(read.Content.Headers.ContentDisposition.FileName)
            .Contains("page.html");
    }

    [Test]
    public async Task GetFile_WithUnsafeContentTypeAndNoFileName_FallsBackToRecordId(
        CancellationToken ct
    )
    {
        var (client, http) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(
            client,
            FileRecordInput(
                content: SampleContent,
                fileName: null,
                contentType: "text/html"
            )
        );

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(SampleContent), "file", "page.html");
        using var upload = await http.PostAsync($"api/files/{id}", form, ct);
        await Assert.That(upload.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        using var read = await http.GetAsync($"api/files/{id}", ct);

        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert
            .That(read.Content.Headers.ContentDisposition!.FileName)
            .Contains(id.ToString());
    }

    [Test]
    public async Task GetFile_BeforeUpload_ReturnsNotFound(CancellationToken ct)
    {
        var (client, http) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(client, FileRecordInput());

        using var read = await http.GetAsync($"api/files/{id}", ct);

        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UploadFile_WhenAlreadyUploaded_ReturnsConflict(CancellationToken ct)
    {
        var (client, http) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(client, FileRecordInput());

        using var first = new MultipartFormDataContent();
        first.Add(new ByteArrayContent(SampleContent), "file", "book.epub");
        using var firstResponse = await http.PostAsync($"api/files/{id}", first, ct);
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        using var second = new MultipartFormDataContent();
        second.Add(new ByteArrayContent(SampleContent), "file", "book.epub");
        using var secondResponse = await http.PostAsync($"api/files/{id}", second, ct);

        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task UploadFile_WhenContentMismatchesDeclaredHash_ReturnsUnprocessableEntity(
        CancellationToken ct
    )
    {
        var (client, http) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(
            client,
            FileRecordInput(content: SampleContent, sha256: Sha256Hex([7, 7, 7]))
        );

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(SampleContent), "file", "book.epub");
        using var upload = await http.PostAsync($"api/files/{id}", form, ct);

        await Assert
            .That(upload.StatusCode)
            .IsEqualTo(HttpStatusCode.UnprocessableEntity);

        using var read = await http.GetAsync($"api/files/{id}", ct);
        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetFile_OtherUsersRecord_ReturnsNotFoundAndQueryIsScoped(
        CancellationToken ct
    )
    {
        var (ownerClient, ownerHttp) = await AuthenticatedClients("owner");
        var id = await AddFileRecordAsync(ownerClient, FileRecordInput());

        using var upload = new MultipartFormDataContent();
        upload.Add(new ByteArrayContent(SampleContent), "file", "book.epub");
        using var uploadResponse = await ownerHttp.PostAsync(
            $"api/files/{id}",
            upload,
            ct
        );
        await Assert.That(uploadResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var (otherClient, otherHttp) = await AuthenticatedClients("other-user");

        using var read = await otherHttp.GetAsync($"api/files/{id}", ct);
        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        var query = await otherClient.Query(q =>
            q.FileRecords(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: null,
                order: null,
                selector: c => c.Nodes(f => f.Id)
            )
        );
        await Assert.That(query.Errors).IsNull().Or.IsEmpty();
        await Assert.That(query.Data!).IsEmpty();
    }

    [Test]
    public async Task UploadFile_WithUnknownId_ReturnsNotFound(CancellationToken ct)
    {
        var (_, http) = await AuthenticatedClients();

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(SampleContent), "file", "book.epub");
        using var upload = await http.PostAsync(
            $"api/files/{Guid.CreateVersion7().WithPostfix(0xFF)}",
            form,
            ct
        );

        await Assert.That(upload.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
// Mostly Ai Generated - End
