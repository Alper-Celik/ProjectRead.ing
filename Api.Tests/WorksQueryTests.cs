// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Threading.Tasks;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

public class WorksQueryTests : WorksTestBase
{
    [Test]
    public async Task Authors_CanBeFilteredAndSorted(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        await AddAuthor(client, displayName: "Zed");
        await AddAuthor(client, displayName: "Alice");

        var order = new[] { new AuthorSortInput { DisplayName = SortEnumType.Asc } };
        var filter = new AuthorFilterInput
        {
            DisplayName = new StringOperationFilterInput { Neq = "Nobody" },
        };

        var result = await client.Query(q =>
            q.Authors(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: order,
                selector: c => new
                {
                    c.TotalCount,
                    Nodes = c.Nodes(a => new { a.DisplayName }),
                }
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert
            .That(result.Data!.Nodes!.Select(a => a.DisplayName))
            .IsEquivalentTo(["Alice", "Zed"]);
        await Assert.That(result.Data.TotalCount).IsEqualTo(2);
    }

    [Test]
    public async Task AuthorWorks_ReturnsOnlyThatAuthorsWorks(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (authorId, _) = await AddAuthor(client, displayName: "Solo");
        var (otherId, _) = await AddAuthor(client, displayName: "Other");
        await AddWork(client, title: "By Solo", authorIds: [authorId]);
        await AddWork(client, title: "By Other", authorIds: [otherId]);

        var result = await client.Query(q =>
            q.Authors(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: null,
                order: null,
                selector: c =>
                    c.Nodes(a => new
                    {
                        a.DisplayName,
                        Works = a.Works(
                            first: 10,
                            after: null,
                            last: null,
                            before: null,
                            where: null,
                            order: null,
                            selector: w => w.Nodes(x => new { x.Title })
                        ),
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var authors = result.Data!.ToDictionary(a => a.DisplayName);
        await Assert.That(authors["Solo"].Works).HasSingleItem();
        await Assert.That(authors["Solo"].Works![0].Title).IsEqualTo("By Solo");
        await Assert.That(authors["Other"].Works).HasSingleItem();
        await Assert.That(authors["Other"].Works![0].Title).IsEqualTo("By Other");
    }

    [Test]
    public async Task Tags_CanBeFilteredAndSorted(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        await AddTag(client, tagName: "Zeta");
        await AddTag(client, tagName: "Alpha");

        var order = new[] { new TagSortInput { TagName = SortEnumType.Asc } };
        var filter = new TagFilterInput
        {
            TagName = new StringOperationFilterInput { Contains = "a" },
        };

        var result = await client.Query(q =>
            q.Tags(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: order,
                selector: c => new
                {
                    c.TotalCount,
                    Nodes = c.Nodes(t => new { t.TagName }),
                }
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert
            .That(result.Data!.Nodes!.Select(t => t.TagName))
            .IsEquivalentTo(["Alpha", "Zeta"]);
        await Assert.That(result.Data.TotalCount).IsEqualTo(2);
    }

    [Test]
    public async Task Works_CanBeFilteredAndSorted(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        await AddWork(client, title: "Zeta");
        await AddWork(client, title: "Alpha");

        var order = new[] { new WorkSortInput { Title = SortEnumType.Asc } };
        var filter = new WorkFilterInput
        {
            Title = new StringOperationFilterInput { Contains = "a" },
        };

        var result = await client.Query(q =>
            q.Works(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: order,
                selector: c => new { c.TotalCount, Nodes = c.Nodes(w => new { w.Title }) }
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert
            .That(result.Data!.Nodes!.Select(w => w.Title))
            .IsEquivalentTo(["Alpha", "Zeta"]);
        await Assert.That(result.Data.TotalCount).IsEqualTo(2);
    }

    [Test]
    public async Task Node_ResolvesAuthorTagAndWork(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (authorId, _) = await AddAuthor(client);
        var (tagId, _) = await AddTag(client);
        var (workId, _) = await AddWork(client);

        var author = await client.Query(q => q.Node(authorId, n => n.Id));
        var tag = await client.Query(q => q.Node(tagId, n => n.Id));
        var work = await client.Query(q => q.Node(workId, n => n.Id));

        await Assert.That(author.Errors).IsNull().Or.IsEmpty();
        await Assert.That(tag.Errors).IsNull().Or.IsEmpty();
        await Assert.That(work.Errors).IsNull().Or.IsEmpty();

        await Assert.That(author.Data!.Value).IsEqualTo(authorId.ToString("D"));
        await Assert.That(tag.Data!.Value).IsEqualTo(tagId.ToString("D"));
        await Assert.That(work.Data!.Value).IsEqualTo(workId.ToString("D"));
    }

    [Test]
    public async Task Node_WithUnknownId_ReturnsInvalidIdError(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var unknownId = Guid.CreateVersion7().WithPostfix(0xFF);

        var result = await client.Query(q => q.Node(unknownId, n => n.Id));

        await AssertErrorCode(result, ErrorCodes.INVALID_ID);
    }

    [Test]
    public async Task AuthorWorks_ArePaginated(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (authorId, _) = await AddAuthor(client);
        await AddWork(client, title: "First", authorIds: [authorId]);
        await AddWork(client, title: "Second", authorIds: [authorId]);

        var order = new[] { new WorkSortInput { Title = SortEnumType.Asc } };

        var result = await client.Query(q =>
            q.Authors(
                first: 1,
                after: null,
                last: null,
                before: null,
                where: null,
                order: null,
                selector: c =>
                    c.Nodes(a => new
                    {
                        Works = a.Works(
                            first: 1,
                            after: null,
                            last: null,
                            before: null,
                            where: null,
                            order: order,
                            selector: w => new
                            {
                                w.TotalCount,
                                Nodes = w.Nodes(x => new { x.Title }),
                            }
                        ),
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var page = result.Data!.Single().Works;
        await Assert.That(page.TotalCount).IsEqualTo(2);
        await Assert.That(page.Nodes).HasSingleItem();
        await Assert.That(page.Nodes![0].Title).IsEqualTo("First");
    }
}
// Mostly Ai Generated - End
