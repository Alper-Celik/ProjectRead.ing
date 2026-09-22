// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Net;
using System.Threading.Tasks;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

public class FilesMutationTests : FilesTestBase
{
    [Test]
    public async Task AddFileRecord_PersistsMetadataWithoutContent(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var id = await AddFileRecordAsync(
            client,
            FileRecordInput(fileName: null, sizeBytes: SampleContent.LongLength)
        );

        var filter = new FileRecordFilterInput
        {
            Id = new UuidOperationFilterInput { Eq = id },
        };

        var result = await client.Query(q =>
            q.FileRecords(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: null,
                selector: c => c.Nodes(f => new { f.Uploaded, f.OriginalFileName })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var record = result.Data!.Single();
        await Assert.That(record.Uploaded).IsFalse();
        await Assert.That(record.OriginalFileName).IsNull();
    }

    [Test]
    public async Task AddFileRecord_WithMalformedSha256_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new { input = FileRecordInput(sha256: new string('a', 63)) },
            static (i, m) =>
                m.AddFileRecordMutation(i.input, p => p.FileRecord(f => f.Id))
        );

        await AssertErrorCode(result, ErrorCodes.INVALID_SHA256);
    }

    [Test]
    public async Task AddFileRecordWithFile_StoresContentAndMarksUploaded(
        CancellationToken ct
    )
    {
        var (client, http) = await AuthenticatedClients();

        var result = await client.Mutation(
            new
            {
                input = FileRecordInput(fileName: null),
                file = UploadOf(SampleContent, "from-upload.epub"),
            },
            static (i, m) =>
                m.AddFileRecordWithFileMutation(
                    i.input,
                    i.file,
                    p => p.FileRecord(f => new { f.Id, f.Uploaded })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.Uploaded).IsTrue();

        var id = NodeIdToGuid(result.Data.Id);
        var response = await http.GetAsync($"api/files/{id}", ct);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert
            .That(await response.Content.ReadAsByteArrayAsync(ct))
            .IsEquivalentTo(SampleContent);
    }

    [Test]
    public async Task AddFileRecordWithFile_WhenContentMismatchesDeclaredHash_RemovesRecord(
        CancellationToken ct
    )
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = FileRecordInput(sha256: Sha256Hex([9, 9, 9])),
                file = UploadOf(SampleContent),
            },
            static (i, m) =>
                m.AddFileRecordWithFileMutation(
                    i.input,
                    i.file,
                    p => p.FileRecord(f => f.Id)
                )
        );

        await AssertErrorCode(result, ErrorCodes.FILE_UPLOAD_FAILED);

        var remaining = await client.Query(q =>
            q.FileRecords(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: null,
                order: null,
                selector: c => c.Nodes(f => new { f.Id })
            )
        );

        await Assert.That(remaining.Errors).IsNull().Or.IsEmpty();
        await Assert.That(remaining.Data!).IsEmpty();
    }

    [Test]
    public async Task UploadFileRecordContent_StoresContentOfReservedRecord(
        CancellationToken ct
    )
    {
        var (client, http) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(client, FileRecordInput());

        var result = await client.Mutation(
            new
            {
                input = new UploadFileRecordContentInput { Id = id },
                file = UploadOf(SampleContent),
            },
            static (i, m) =>
                m.UploadFileRecordContentMutation(
                    i.input,
                    i.file,
                    p =>
                        p.FileRecord(f => new
                        {
                            f.Id,
                            f.Uploaded,
                            f.RowVersion,
                        })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.Uploaded).IsTrue();
        await Assert.That(result.Data.RowVersion).IsEqualTo(1);

        var response = await http.GetAsync($"api/files/{id}", ct);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert
            .That(await response.Content.ReadAsByteArrayAsync(ct))
            .IsEquivalentTo(SampleContent);
    }

    [Test]
    public async Task UploadFileRecordContent_WhenAlreadyUploaded_Fails(
        CancellationToken ct
    )
    {
        var (client, _) = await AuthenticatedClients();
        var id = await AddFileRecordAsync(client, FileRecordInput());

        var first = await client.Mutation(
            new
            {
                input = new UploadFileRecordContentInput { Id = id },
                file = UploadOf(SampleContent),
            },
            static (i, m) =>
                m.UploadFileRecordContentMutation(
                    i.input,
                    i.file,
                    p => p.FileRecord(f => f.Id)
                )
        );
        await Assert.That(first.Errors).IsNull().Or.IsEmpty();

        var second = await client.Mutation(
            new
            {
                input = new UploadFileRecordContentInput { Id = id },
                file = UploadOf(SampleContent),
            },
            static (i, m) =>
                m.UploadFileRecordContentMutation(
                    i.input,
                    i.file,
                    p => p.FileRecord(f => f.Id)
                )
        );

        await AssertErrorCode(second, ErrorCodes.FILE_ALREADY_UPLOADED);
    }

    [Test]
    public async Task UploadFileRecordContent_WithUnknownId_Fails(CancellationToken ct)
    {
        var (client, _) = await AuthenticatedClients();
        var unknownId = Guid.CreateVersion7().WithPostfix(0xFF);

        var result = await client.Mutation(
            new
            {
                input = new UploadFileRecordContentInput { Id = unknownId },
                file = UploadOf(SampleContent),
            },
            static (i, m) =>
                m.UploadFileRecordContentMutation(
                    i.input,
                    i.file,
                    p => p.FileRecord(f => f.Id)
                )
        );

        await AssertErrorCode(result, ErrorCodes.ID_DOES_NOT_EXIST);
    }
}
// Mostly Ai Generated - End
