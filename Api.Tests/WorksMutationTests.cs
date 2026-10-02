// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Threading.Tasks;
using Api.Database;
using Api.Utils;
using Api.Works.Models;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZeroQL;
using ZeroQL.Client;
using static Api.Utils.GeneralUtils;
using ClientDomainError = ZeroQL.Client.DomainError;

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
                        n.MetadataAddedAt,
                        n.MetadataUpdatedAt,
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var node = result.Data!.Single();
        await Assert.That(node.FirstName).IsEqualTo("Jane");
        await Assert.That(node.LastName).IsEqualTo("Doe");
        await Assert.That(node.DisplayName).IsEqualTo("Jane Doe");
        await Assert.That(node.PenNames).IsEquivalentTo(["J. D.", "Janey"]);
        await Assert.That(node.MetadataAddedAt).IsEqualTo(node.MetadataUpdatedAt);
    }

    [Test]
    public async Task UpdateAuthor_UpdatesProvidedFields(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, rowVersion) = await AddAuthor(client, displayName: "Old Name");

        var filter = new AuthorFilterInput
        {
            Id = new UuidOperationFilterInput { Eq = id },
        };

        var before = await client.Query(q =>
            q.Authors(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: null,
                selector: c =>
                    c.Nodes(n => new { n.MetadataAddedAt, n.MetadataUpdatedAt })
            )
        );

        await Assert.That(before.Errors).IsNull().Or.IsEmpty();
        var beforeNode = before.Data!.Single();

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
                m.UpdateAuthor(
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
                            a.MetadataAddedAt,
                            a.MetadataUpdatedAt,
                        })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.DisplayName).IsEqualTo("New Name");
        await Assert.That(result.Data.FirstName).IsEqualTo("New");
        await Assert.That(result.Data.LastName).IsEqualTo("Name");
        await Assert.That(result.Data.PenNames).IsEquivalentTo(["NP"]);
        await Assert.That(result.Data.RowVersion).IsEqualTo(rowVersion + 1);
        await Assert
            .That(result.Data.MetadataAddedAt)
            .IsEqualTo(beforeNode.MetadataAddedAt);
        await Assert
            .That(result.Data.MetadataUpdatedAt)
            .IsGreaterThan(beforeNode.MetadataUpdatedAt);
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
                m.UpdateAuthor(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.ID_DOES_NOT_EXIST);
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
                m.UpdateAuthor(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.ROW_VERSION_MISMATCH);
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
                        n.MetadataAddedAt,
                        n.MetadataUpdatedAt,
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var node = result.Data!.Single();
        await Assert.That(node.TagName).IsEqualTo("Sci-Fi");
        await Assert.That(node.TagNamespace).IsEquivalentTo(["genre", "sub"]);
        await Assert.That(node.MetadataAddedAt).IsEqualTo(node.MetadataUpdatedAt);
    }

    [Test]
    public async Task DbExceptionUtils_MapsUniqueViolationToTagAlreadyExists(
        CancellationToken ct
    )
    {
        var client = await AuthenticatedClient();
        await AddTag(client, tagName: "DupRace", tagNamespace: ["race"]);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<PGContext>()!;
        var ownerId = await db
            .Users.Where(u => u.Email == "works@projectread.ing")
            .Select(u => u.Id)
            .SingleAsync(cancellationToken: ct);

        var dupe = new WorkTag
        {
            Id = Guid.CreateVersion7().WithPostfix(IdPostfixes.WorkTag),
            TagName = "DupRace",
            TagNamespace = ["race"],
            OwnerId = ownerId,
            RowVersion = 0,
            MetadataAddedAt = Now(),
            MetadataUpdatedAt = Now(),
        };
        db.Add(dupe);

        var act = async () =>
            await db.SaveChangesOrThrowAsync(
                PostgresErrorCodes.UniqueViolation,
                ErrorCodes.TAG_ALREADY_EXISTS,
                "A tag with the same namespace and name already exists",
                ct
            );
        var exception = await Assert.That(act).Throws<DomainException>();
        await Assert.That(exception?.Code).IsEqualTo(ErrorCodes.TAG_ALREADY_EXISTS);
    }

    // Mostly Ai Generated - Start
    [Test]
    public async Task DbConstraints_RejectInvalidTagComponents(
        CancellationToken ct
    )
    {
        var client = await AuthenticatedClient();
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<PGContext>()!;
        var ownerId = await db.Users.Select(u => u.Id).FirstAsync(ct);

        var badName = new WorkTag
        {
            Id = Guid.CreateVersion7().WithPostfix(IdPostfixes.WorkTag),
            TagName = "a::b",
            TagNamespace = ["genre"],
            OwnerId = ownerId,
            RowVersion = 0,
            MetadataAddedAt = Now(),
            MetadataUpdatedAt = Now(),
        };
        db.Add(badName);
        var actName = async () => await db.SaveChangesAsync(ct);
        await Assert.That(actName).Throws<DbUpdateException>();
        db.Entry(badName).State = EntityState.Detached;

        var badNamespace = new WorkTag
        {
            Id = Guid.CreateVersion7().WithPostfix(IdPostfixes.WorkTag),
            TagName = "Valid",
            TagNamespace = ["a::b"],
            OwnerId = ownerId,
            RowVersion = 0,
            MetadataAddedAt = Now(),
            MetadataUpdatedAt = Now(),
        };
        db.Add(badNamespace);
        var actNamespace = async () => await db.SaveChangesAsync(ct);
        await Assert.That(actNamespace).Throws<DbUpdateException>();
    }
    // Mostly Ai Generated - End

    [Test]
    public async Task AddTag_WithDuplicatePair_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        await AddTag(client, tagName: "Sci-Fi", tagNamespace: ["genre", "sub"]);

        var result = await client.Mutation(
            new
            {
                input = new AddTagInput
                {
                    TagName = "Sci-Fi",
                    TagNamespace = ["genre", "sub"],
                },
            },
            static (i, m) =>
                m.AddTag(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.TAG_ALREADY_EXISTS);
    }

    // Mostly Ai Generated - Start
    [Test]
    [Arguments("a::b")]
    [Arguments("")]
    [Arguments(":a")]
    [Arguments("a:")]
    [Arguments("a::")]
    [Arguments("::a")]
    public async Task AddTag_WithInvalidNamespaceComponent_Fails(
        string component
    )
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = new AddTagInput
                {
                    TagName = "Valid",
                    TagNamespace = ["genre", component],
                },
            },
            static (i, m) =>
                m.AddTag(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(
            result,
            ErrorCodes.TAG_FORBIDDEN_SEPARATOR
        );
    }

    [Test]
    [Arguments("a::b")]
    [Arguments("")]
    [Arguments(":a")]
    [Arguments("a:")]
    public async Task AddTag_WithInvalidTagName_Fails(string tagName)
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = new AddTagInput
                {
                    TagName = tagName,
                    TagNamespace = ["genre"],
                },
            },
            static (i, m) =>
                m.AddTag(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(
            result,
            ErrorCodes.TAG_FORBIDDEN_SEPARATOR
        );
    }

    [Test]
    public async Task AddTag_WithSingleColonComponent_Succeeds()
    {
        var client = await AuthenticatedClient();

        var result = await client.Mutation(
            new
            {
                input = new AddTagInput
                {
                    TagName = "Name:Sub",
                    TagNamespace = ["genre"],
                },
            },
            static (i, m) => m.AddTag(i.input, p => p.Tag(t => t.TagName))
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data).IsEqualTo("Name:Sub");
    }

    [Test]
    public async Task UpdateTag_WithInvalidComponent_Fails()
    {
        var client = await AuthenticatedClient();
        var (id, rowVersion) = await AddTag(
            client,
            tagName: "Old",
            tagNamespace: ["genre"]
        );

        var result = await client.Mutation(
            new
            {
                input = new UpdateTagInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    TagName = "a::b",
                    TagNamespace = ["genre"],
                },
            },
            static (i, m) =>
                m.UpdateTag(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(
            result,
            ErrorCodes.TAG_FORBIDDEN_SEPARATOR
        );
    }

    [Test]
    public async Task TagFullName_RoundTripsLosslessly()
    {
        var ns = new[] { "ids", "genre:sub" };
        var fullName = GeneralUtils.ToTagFullName(ns, "isbn:10");

        await Assert.That(fullName).IsEqualTo("ids::genre:sub::isbn:10");

        var split = GeneralUtils.FromTagFullName(fullName);
        await Assert.That(split[..^1]).IsEquivalentTo(ns);
        await Assert.That(split[^1]).IsEqualTo("isbn:10");
    }
    // Mostly Ai Generated - End

    [Test]
    public async Task UpdateTag_UpdatesProvidedFields(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, rowVersion) = await AddTag(client, tagName: "Old");

        var filter = new TagFilterInput { Id = new UuidOperationFilterInput { Eq = id } };

        var before = await client.Query(q =>
            q.Tags(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: null,
                selector: c =>
                    c.Nodes(n => new { n.MetadataAddedAt, n.MetadataUpdatedAt })
            )
        );

        await Assert.That(before.Errors).IsNull().Or.IsEmpty();
        var beforeNode = before.Data!.Single();

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
                m.UpdateTag(
                    i.input,
                    p =>
                        p.Tag(t => new
                        {
                            t.Id,
                            t.TagName,
                            t.TagNamespace,
                            t.RowVersion,
                            t.MetadataAddedAt,
                            t.MetadataUpdatedAt,
                        })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data!.TagName).IsEqualTo("New");
        await Assert.That(result.Data.TagNamespace).IsEquivalentTo(["new_ns"]);
        await Assert.That(result.Data.RowVersion).IsEqualTo(rowVersion + 1);
        await Assert
            .That(result.Data.MetadataAddedAt)
            .IsEqualTo(beforeNode.MetadataAddedAt);
        await Assert
            .That(result.Data.MetadataUpdatedAt)
            .IsGreaterThan(beforeNode.MetadataUpdatedAt);
    }

    [Test]
    public async Task UpdateTag_ToExistingPair_Fails(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        await AddTag(client, tagName: "Existing", tagNamespace: ["genre"]);
        var (id, rowVersion) = await AddTag(
            client,
            tagName: "Other",
            tagNamespace: ["genre"]
        );

        var result = await client.Mutation(
            new
            {
                input = new UpdateTagInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    TagName = "Existing",
                    TagNamespace = ["genre"],
                },
            },
            static (i, m) =>
                m.UpdateTag(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.TAG_ALREADY_EXISTS);
    }

    [Test]
    public async Task UpdateTag_WithUnchangedPair_Succeeds(CancellationToken ct)
    {
        var client = await AuthenticatedClient();
        var (id, rowVersion) = await AddTag(
            client,
            tagName: "Same",
            tagNamespace: ["genre"]
        );

        var result = await client.Mutation(
            new
            {
                input = new UpdateTagInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    TagName = "Same",
                    TagNamespace = ["genre"],
                },
            },
            static (i, m) => m.UpdateTag(i.input, p => p.Tag(t => t.TagName))
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data).IsEqualTo("Same");
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
            static (i, m) =>
                m.UpdateTag(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.ID_DOES_NOT_EXIST);
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
            static (i, m) =>
                m.UpdateTag(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.ROW_VERSION_MISMATCH);
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
                        n.MetadataAddedAt,
                        n.MetadataUpdatedAt,
                        Authors = n.Authors(a => new { a.DisplayName }),
                    })
            )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        var node = result.Data!.Single();
        await Assert.That(node.Title).IsEqualTo("The Work");
        await Assert.That(node.Description).IsEqualTo("A description");
        await Assert.That(node.WorkPublishedAt).IsNotNull();
        await Assert.That(node.WorkUpdatedAt).IsNotNull();
        await Assert.That(node.MetadataAddedAt).IsEqualTo(node.MetadataUpdatedAt);
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
                },
            },
            static (i, m) =>
                m.AddWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.IDS_DOES_NOT_EXIST);
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
                },
            },
            static (i, m) =>
                m.AddWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.IDS_DOES_NOT_EXIST);
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
                },
            },
            static (i, m) =>
                m.AddWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.IS_NOT_DISTINCT);
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
                },
            },
            static (i, m) =>
                m.AddWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.IS_NOT_DISTINCT);
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
            tagIds: [tagA],
            authorIds: [authorA]
        );

        var filter = new WorkFilterInput
        {
            Id = new UuidOperationFilterInput { Eq = id },
        };

        var before = await client.Query(q =>
            q.Works(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: null,
                selector: c =>
                    c.Nodes(n => new { n.MetadataAddedAt, n.MetadataUpdatedAt })
            )
        );

        await Assert.That(before.Errors).IsNull().Or.IsEmpty();
        var beforeNode = before.Data!.Single();

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
                    TagIds = [tagB],
                    AuthorIds = [authorB],
                },
            },
            static (i, m) =>
                m.UpdateWork(
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
                            w.MetadataAddedAt,
                            w.MetadataUpdatedAt,
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
        await Assert
            .That(result.Data.MetadataAddedAt)
            .IsEqualTo(beforeNode.MetadataAddedAt);
        await Assert
            .That(result.Data.MetadataUpdatedAt)
            .IsGreaterThan(beforeNode.MetadataUpdatedAt);
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
            tagIds: [tagId],
            authorIds: [authorId]
        );

        var filter = new WorkFilterInput
        {
            Id = new UuidOperationFilterInput { Eq = id },
        };

        var before = await client.Query(q =>
            q.Works(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: filter,
                order: null,
                selector: c =>
                    c.Nodes(n => new { n.MetadataAddedAt, n.MetadataUpdatedAt })
            )
        );

        await Assert.That(before.Errors).IsNull().Or.IsEmpty();
        var beforeNode = before.Data!.Single();

        var result = await client.Mutation(
            new
            {
                input = new UpdateWorkInput
                {
                    Id = id,
                    RowVersion = rowVersion,
                    Title = "Original",
                    TagIds = [],
                    AuthorIds = [],
                },
            },
            static (i, m) =>
                m.UpdateWork(
                    i.input,
                    p =>
                        p.Work(w => new
                        {
                            w.MetadataAddedAt,
                            w.MetadataUpdatedAt,
                            Authors = w.Authors(a => a.DisplayName),
                        })
                )
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert.That(result.Data.Authors).IsEmpty();
        await Assert
            .That(result.Data.MetadataAddedAt)
            .IsEqualTo(beforeNode.MetadataAddedAt);
        await Assert
            .That(result.Data.MetadataUpdatedAt)
            .IsGreaterThan(beforeNode.MetadataUpdatedAt);
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
            static (i, m) =>
                m.UpdateWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.ID_DOES_NOT_EXIST);
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
            static (i, m) =>
                m.UpdateWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.ROW_VERSION_MISMATCH);
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
            static (i, m) =>
                m.UpdateWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.IDS_DOES_NOT_EXIST);
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
            static (i, m) =>
                m.UpdateWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.IDS_DOES_NOT_EXIST);
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
            static (i, m) =>
                m.UpdateWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.IS_NOT_DISTINCT);
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
            static (i, m) =>
                m.UpdateWork(
                    i.input,
                    p => p.Errors(e => e.On<ClientDomainError>().Select(x => x.Code))
                )
        );

        await AssertDomainErrorCode(result, ErrorCodes.IS_NOT_DISTINCT);
    }
}
// Mostly Ai Generated - End
