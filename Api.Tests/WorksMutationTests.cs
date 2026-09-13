// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Threading.Tasks;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

public class WorksMutationTests : WorksTestBase
{
    [Test]
    public async Task AddAuthor_PersistsAuthor(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var (id, _) = await AddAuthor(
            client,
            displayName: "Jane Doe",
            firstName: "Jane",
            lastName: "Doe",
            penNames: ["J. D.", "Janey"]
        );

        var filter = new AuthorFilterInput
        {
            Id = new UuidOperationFilterInput { Eq = id },
        };

        var result = await client.Query(q =>
            q.Authors(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: null,
                selector: c =>
                    c.Nodes(n => new
                    {
                        n.Id,
                        n.FirstName,
                        n.LastName,
                        n.DisplayName,
                        n.PenNames,
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var node = result.Data!.Single();
        await Assert.That(node.FirstName).IsEqualTo("Jane");
        await Assert.That(node.LastName).IsEqualTo("Doe");
        await Assert.That(node.DisplayName).IsEqualTo("Jane Doe");
        await Assert.That(node.PenNames).IsEquivalentTo(["J. D.", "Janey"]);
    }

    [Test]
    public async Task UpdateAuthor_UpdatesProvidedFields(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, rowVersion) = await AddAuthor(client, displayName: "Old Name");

        var result = await client.Mutation(
            new
            {
                input = new UpdateAuthorInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    DisplayName = "New Name",
                    FirstName = "New",
                    LastName = "Name",
                    PenNames = ["NP"],
                },
            },
            static (i, m) =>
                m.UpdateAuthorMutation(
                    i.input,
                    p =>
                        p.Author(a => new
                        {
                            a.Id,
                            a.DisplayName,
                            a.FirstName,
                            a.LastName,
                            a.PenNames,
                            a.RowVersion,
                        })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.DisplayName).IsEqualTo("New Name");
        await Assert.That(result.Data.FirstName).IsEqualTo("New");
        await Assert.That(result.Data.LastName).IsEqualTo("Name");
        await Assert.That(result.Data.PenNames).IsEquivalentTo(["NP"]);
        await Assert.That(result.Data.RowVersion).IsEqualTo(rowVersion + 1);
    }

    [Test]
    public async Task UpdateAuthor_WithUnknownId_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = new UpdateAuthorInput
                {
                    Id = Guid.CreateVersion7(),
                    RowVersion = 0,
                    DisplayName = "Nope",
                },
            },
            static (i, m) =>
                m.UpdateAuthorMutation(i.input, p => p.Author(a => a.DisplayName))
        );

        await AssertErrorCode(result, ErrorCodes.ID_DOES_NOT_EXIST);
    }

    [Test]
    public async Task UpdateAuthor_WithStaleRowVersion_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, _) = await AddAuthor(client, displayName: "Old Name");

        var result = await client.Mutation(
            new
            {
                input = new UpdateAuthorInput
                {
                    Id = id,
                    RowVersion = 42,
                    DisplayName = "New Name",
                },
            },
            static (i, m) =>
                m.UpdateAuthorMutation(i.input, p => p.Author(a => a.DisplayName))
        );

        await AssertErrorCode(result, ErrorCodes.ROW_VERSION_MISMATCH);
    }

    [Test]
    public async Task AddTag_PersistsTag(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var (id, _) = await AddTag(
            client,
            tagName: "Sci-Fi",
            tagNamespace: ["genre", "sub"]
        );

        var filter = new TagFilterInput { Id = new UuidOperationFilterInput { Eq = id } };

        var result = await client.Query(q =>
            q.Tags(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: null,
                selector: c =>
                    c.Nodes(n => new
                    {
                        n.Id,
                        n.TagName,
                        n.TagNamespace,
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var node = result.Data!.Single();
        await Assert.That(node.TagName).IsEqualTo("Sci-Fi");
        await Assert.That(node.TagNamespace).IsEquivalentTo(["genre", "sub"]);
    }

    [Test]
    public async Task UpdateTag_UpdatesProvidedFields(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, rowVersion) = await AddTag(client, tagName: "Old");

        var result = await client.Mutation(
            new
            {
                input = new UpdateTagInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    TagName = "New",
                    TagNamespace = ["new_ns"],
                },
            },
            static (i, m) =>
                m.UpdateTagMutation(
                    i.input,
                    p =>
                        p.Tag(t => new
                        {
                            t.Id,
                            t.TagName,
                            t.TagNamespace,
                            t.RowVersion,
                        })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.TagName).IsEqualTo("New");
        await Assert.That(result.Data.TagNamespace).IsEquivalentTo(["new_ns"]);
        await Assert.That(result.Data.RowVersion).IsEqualTo(rowVersion + 1);
    }

    [Test]
    public async Task UpdateTag_WithUnknownId_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = new UpdateTagInput
                {
                    Id = Guid.CreateVersion7(),
                    RowVersion = 0,
                    TagName = "Nope",
                    TagNamespace = ["nope"],
                },
            },
            static (i, m) => m.UpdateTagMutation(i.input, p => p.Tag(t => t.TagName))
        );

        await AssertErrorCode(result, ErrorCodes.ID_DOES_NOT_EXIST);
    }

    [Test]
    public async Task UpdateTag_WithStaleRowVersion_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, _) = await AddTag(client, tagName: "Old");

        var result = await client.Mutation(
            new
            {
                input = new UpdateTagInput
                {
                    Id = id,
                    RowVersion = 42,
                    TagName = "New",
                    TagNamespace = ["new"],
                },
            },
            static (i, m) => m.UpdateTagMutation(i.input, p => p.Tag(t => t.TagName))
        );

        await AssertErrorCode(result, ErrorCodes.ROW_VERSION_MISMATCH);
    }

    [Test]
    public async Task AddWork_PersistsWorkWithRelations(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (authorId, _) = await AddAuthor(client, displayName: "Author One");
        var (tagId, _) = await AddTag(client, tagName: "Genre");

        var publishedAt = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedAt = DateTimeOffset.UtcNow;

        var (id, _) = await AddWork(
            client,
            title: "The Work",
            description: "A description",
            publishedAt: publishedAt,
            updatedAt: updatedAt,
            identifiers:
            [
                new WorkIdentifierInput
                {
                    WorkIdentifierType = "isbn",
                    WorkIdentifierValue = "123",
                },
            ],
            tagIds: [tagId],
            authorIds: [authorId]
        );

        var filter = new WorkFilterInput
        {
            Id = new UuidOperationFilterInput { Eq = id },
        };

        var result = await client.Query(q =>
            q.Works(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: null,
                selector: c =>
                    c.Nodes(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Description,
                        n.WorkPublishedAt,
                        n.WorkUpdatedAt,
                        Authors = n.Authors(a => new { a.DisplayName }),
                        Identifiers = n.WorkIdentifiers(w => new
                        {
                            w.WorkIdentifierType,
                            w.WorkIdentifierValue,
                        }),
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var node = result.Data!.Single();
        await Assert.That(node.Title).IsEqualTo("The Work");
        await Assert.That(node.Description).IsEqualTo("A description");
        await Assert.That(node.WorkPublishedAt).IsNotNull();
        await Assert.That(node.WorkUpdatedAt).IsNotNull();
        await Assert.That(node.Identifiers).HasSingleItem();
        await Assert.That(node.Identifiers[0].WorkIdentifierType).IsEqualTo("isbn");
        await Assert.That(node.Identifiers[0].WorkIdentifierValue).IsEqualTo("123");
        await Assert.That(node.Authors).HasSingleItem();
        await Assert.That(node.Authors[0].DisplayName).IsEqualTo("Author One");
    }

    [Test]
    public async Task AddWork_WithUnknownTagId_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = new AddWorkInput
                {
                    Title = "Bad",
                    TagIds = [Guid.CreateVersion7()],
                    AuthorIds = [],
                    WorkIdentifiers = [],
                },
            },
            static (i, m) => m.AddWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.IDS_DOES_NOT_EXIST);
    }

    [Test]
    public async Task AddWork_WithUnknownAuthorId_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = new AddWorkInput
                {
                    Title = "Bad",
                    TagIds = [],
                    AuthorIds = [Guid.CreateVersion7()],
                    WorkIdentifiers = [],
                },
            },
            static (i, m) => m.AddWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.IDS_DOES_NOT_EXIST);
    }

    [Test]
    public async Task AddWork_WithDuplicateTagIds_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (tagId, _) = await AddTag(client);

        var result = await client.Mutation(
            new
            {
                input = new AddWorkInput
                {
                    Title = "Bad",
                    TagIds = [tagId, tagId],
                    AuthorIds = [],
                    WorkIdentifiers = [],
                },
            },
            static (i, m) => m.AddWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.IS_NOT_DISTINCT);
    }

    [Test]
    public async Task AddWork_WithDuplicateAuthorIds_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (authorId, _) = await AddAuthor(client);

        var result = await client.Mutation(
            new
            {
                input = new AddWorkInput
                {
                    Title = "Bad",
                    TagIds = [],
                    AuthorIds = [authorId, authorId],
                    WorkIdentifiers = [],
                },
            },
            static (i, m) => m.AddWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.IS_NOT_DISTINCT);
    }

    [Test]
    public async Task UpdateWork_UpdatesProvidedFields(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (authorA, _) = await AddAuthor(client, displayName: "Author A");
        var (authorB, _) = await AddAuthor(client, displayName: "Author B");
        var (tagA, _) = await AddTag(client, tagName: "Tag A");
        var (tagB, _) = await AddTag(client, tagName: "Tag B");

        var (id, rowVersion) = await AddWork(
            client,
            title: "Original",
            description: "Original description",
            identifiers:
            [
                new WorkIdentifierInput
                {
                    WorkIdentifierType = "isbn",
                    WorkIdentifierValue = "old",
                },
            ],
            tagIds: [tagA],
            authorIds: [authorA]
        );

        var publishedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    Title = "Changed",
                    Description = "Changed description",
                    WorkPublishedAt = publishedAt,
                    WorkUpdatedAt = updatedAt,
                    WorkIdentifiers =
                    [
                        new WorkIdentifierInput
                        {
                            WorkIdentifierType = "isbn",
                            WorkIdentifierValue = "new",
                        },
                    ],
                    TagIds = [tagB],
                    AuthorIds = [authorB],
                },
            },
            static (i, m) =>
                m.UpdateWorkMutation(
                    i.input,
                    p =>
                        p.Work(w => new
                        {
                            w.Id,
                            w.Title,
                            w.Description,
                            w.WorkPublishedAt,
                            w.WorkUpdatedAt,
                            w.RowVersion,
                            Identifiers = w.WorkIdentifiers(x => new
                            {
                                x.WorkIdentifierType,
                                x.WorkIdentifierValue,
                            }),
                            Authors = w.Authors(a => new { a.DisplayName }),
                        })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.Title).IsEqualTo("Changed");
        await Assert.That(result.Data.Description).IsEqualTo("Changed description");
        await Assert.That(result.Data.WorkPublishedAt).IsNotNull();
        await Assert.That(result.Data.WorkUpdatedAt).IsNotNull();
        await Assert.That(result.Data.RowVersion).IsEqualTo(rowVersion + 1);
        await Assert.That(result.Data.Identifiers).HasSingleItem();
        await Assert
            .That(result.Data.Identifiers[0].WorkIdentifierValue)
            .IsEqualTo("new");
        await Assert.That(result.Data.Authors).HasSingleItem();
        await Assert.That(result.Data.Authors[0].DisplayName).IsEqualTo("Author B");
    }

    [Test]
    public async Task UpdateWork_CanClearCollections(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (tagId, _) = await AddTag(client);
        var (authorId, _) = await AddAuthor(client);

        var (id, rowVersion) = await AddWork(
            client,
            title: "Original",
            identifiers:
            [
                new WorkIdentifierInput
                {
                    WorkIdentifierType = "isbn",
                    WorkIdentifierValue = "old",
                },
            ],
            tagIds: [tagId],
            authorIds: [authorId]
        );

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    Title = "Original",
                    WorkIdentifiers = [],
                    TagIds = [],
                    AuthorIds = [],
                },
            },
            static (i, m) =>
                m.UpdateWorkMutation(
                    i.input,
                    p =>
                        p.Work(w => new
                        {
                            Identifiers = w.WorkIdentifiers(x => new
                            {
                                x.WorkIdentifierType,
                                x.WorkIdentifierValue,
                            }),
                            Authors = w.Authors(a => a.DisplayName),
                        })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.Identifiers).IsEmpty();
        await Assert.That(result.Data.Authors).IsEmpty();
    }

    [Test]
    public async Task UpdateWork_WithUnknownId_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = Guid.CreateVersion7(),
                    RowVersion = 0,
                    Title = "Nope",
                },
            },
            static (i, m) => m.UpdateWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.ID_DOES_NOT_EXIST);
    }

    [Test]
    public async Task UpdateWork_WithStaleRowVersion_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, _) = await AddWork(client);

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = id,
                    RowVersion = 42,
                    Title = "Nope",
                },
            },
            static (i, m) => m.UpdateWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.ROW_VERSION_MISMATCH);
    }

    [Test]
    public async Task UpdateWork_WithUnknownTagIds_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, rowVersion) = await AddWork(client);

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    Title = "Nope",
                    TagIds = [Guid.CreateVersion7()],
                },
            },
            static (i, m) => m.UpdateWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.IDS_DOES_NOT_EXIST);
    }

    [Test]
    public async Task UpdateWork_WithUnknownAuthorIds_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, rowVersion) = await AddWork(client);

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    Title = "Nope",
                    AuthorIds = [Guid.CreateVersion7()],
                },
            },
            static (i, m) => m.UpdateWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.IDS_DOES_NOT_EXIST);
    }

    [Test]
    public async Task UpdateWork_WithDuplicateTagIds_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (tagId, _) = await AddTag(client);
        var (id, rowVersion) = await AddWork(client);

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    Title = "Nope",
                    TagIds = [tagId, tagId],
                },
            },
            static (i, m) => m.UpdateWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.IS_NOT_DISTINCT);
    }

    [Test]
    public async Task UpdateWork_WithDuplicateAuthorIds_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (authorId, _) = await AddAuthor(client);
        var (id, rowVersion) = await AddWork(client);

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    Title = "Nope",
                    AuthorIds = [authorId, authorId],
                },
            },
            static (i, m) => m.UpdateWorkMutation(i.input, p => p.Work(w => w.Id))
        );

        await AssertErrorCode(result, ErrorCodes.IS_NOT_DISTINCT);
    }
}
// Mostly Ai Generated - End
