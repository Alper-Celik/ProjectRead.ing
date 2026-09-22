// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Threading.Tasks;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

public class FilesQueryTests : FilesTestBase
{
    [Test]
    public async Task FileRecords_ReturnMetadataWithHexSha256(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var id = await AddFileRecordAsync(
            client,
            FileRecordInput(
                fileName: "novel.epub",
                fileKind: FileKind.IngestFile,
                sizeBytes: SampleContent.LongLength + 128
            )
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
                selector: c =>
                    c.Nodes(f => new
                    {
                        f.Id,
                        f.FileKind,
                        f.Uploaded,
                        f.OriginalFileName,
                        f.ContentType,
                        f.SizeBytes,
                        f.Sha256,
                        f.MetadataAddedAt,
                        f.MetadataUpdatedAt,
                        f.FileProviderBackendConfigId,
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var record = result.Data!.Single();
        await Assert.That(NodeIdToGuid(record.Id)).IsEqualTo(id);
        await Assert.That(record.Sha256).IsEqualTo(Sha256Hex(SampleContent));
        await Assert.That(record.Uploaded).IsFalse();
        await Assert.That(record.SizeBytes).IsEqualTo(SampleContent.LongLength + 128);
        await Assert.That(record.OriginalFileName).IsEqualTo("novel.epub");
        await Assert.That(record.ContentType).IsEqualTo(SampleContentType);
        await Assert.That(record.FileKind).IsEqualTo(FileKind.IngestFile);
        await Assert.That(record.MetadataAddedAt).IsEqualTo(record.MetadataUpdatedAt);
        await Assert.That(record.FileProviderBackendConfigId).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task FileRecords_CanBeFilteredAndSorted(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        await AddFileRecordAsync(
            client,
            FileRecordInput(
                [1, 2, 3],
                fileName: "small.epub",
                fileKind: FileKind.UserFile
            )
        );
        await AddFileRecordAsync(
            client,
            FileRecordInput(
                [1, 2, 3, 4, 5, 6],
                fileName: "large.epub",
                fileKind: FileKind.GeneratedFile
            )
        );

        var order = new[] { new FileRecordSortInput { SizeBytes = SortEnumType.Desc } };

        var filter = new FileRecordFilterInput
        {
            FileKind = new FileKindOperationFilterInput { Eq = FileKind.GeneratedFile },
        };

        var generated = await client.Query(q =>
            q.FileRecords(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: order,
                selector: c => c.Nodes(f => new { f.OriginalFileName, f.SizeBytes })
            )
        );

        await Assert.That(generated.Errors).IsNull().Or.IsEmpty();
        var onlyGenerated = generated.Data!.Single();
        await Assert.That(onlyGenerated.OriginalFileName).IsEqualTo("large.epub");

        var all = await client.Query(q =>
            q.FileRecords(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: null,
                order: order,
                selector: c => c.Nodes(f => new { f.OriginalFileName })
            )
        );

        await Assert.That(all.Errors).IsNull().Or.IsEmpty();
        await Assert
            .That(all.Data!.Select(f => f.OriginalFileName!))
            .IsEquivalentTo(["large.epub", "small.epub"]);
    }

    [Test]
    public async Task FileRecords_CanBePaginatedWithCursors(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        await AddFileRecordAsync(
            client,
            FileRecordInput([1, 2, 3], fileName: "first.epub")
        );
        await AddFileRecordAsync(
            client,
            FileRecordInput([1, 2, 3, 4], fileName: "second.epub")
        );

        var order = new[] { new FileRecordSortInput { SizeBytes = SortEnumType.Asc } };

        var first = await client.Query(q =>
            q.FileRecords(
                first: 1,
                after: null,
                last: null,
                before: null,
                where: null,
                order: order,
                selector: c => new
                {
                    Nodes = c.Nodes(f => new { f.OriginalFileName }),
                    PageInfo = c.PageInfo(p => new
                    {
                        p.StartCursor,
                        p.EndCursor,
                        p.HasNextPage,
                    }),
                }
            )
        );

        await Assert.That(first.Errors).IsNull().Or.IsEmpty();
        await Assert.That(first.Data!.Nodes![0].OriginalFileName).IsEqualTo("first.epub");
        await Assert.That(first.Data.PageInfo.HasNextPage).IsTrue();
        await Assert.That(first.Data.PageInfo.StartCursor).IsNotNull();
        await Assert.That(first.Data.PageInfo.EndCursor).IsNotNull();

        var endCursor = first.Data.PageInfo.EndCursor!;
        var second = await client.Query(q =>
            q.FileRecords(
                first: 1,
                after: endCursor,
                last: null,
                before: null,
                where: null,
                order: order,
                selector: c => new { Nodes = c.Nodes(f => new { f.OriginalFileName }) }
            )
        );

        await Assert.That(second.Errors).IsNull().Or.IsEmpty();
        await Assert
            .That(second.Data!.Nodes![0].OriginalFileName)
            .IsEqualTo("second.epub");
    }

    [Test]
    public async Task Node_ResolvesFileRecord(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var id = await AddFileRecordAsync(client, FileRecordInput());

        var result = await client.Query(q => q.Node(id, n => n.Id));

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.Value).IsEqualTo(id.ToString("D"));
    }
}
// Mostly Ai Generated - End
