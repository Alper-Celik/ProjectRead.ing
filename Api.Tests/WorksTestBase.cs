// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

public abstract class WorksTestBase : TestInit
{
    protected const string Password = "correct horse battery staple";

    protected async Task<ApiClient> AuthenticatedClient(
        string name = "works",
        string? password = null
    )
    {
        var httpClient = Factory.CreateClient();
        httpClient.BaseAddress = new Uri(
            httpClient.BaseAddress!.AbsoluteUri + "graphql/"
        );
        var client = new ApiClient(httpClient);

        var result = await Register(client, name, password ?? Password);
        result.HttpResponseMessage.EnsureSuccessStatusCode();
        await Assert.That(result.Errors).IsNull().Or.IsEmpty();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            result.Data!
        );

        return client;
    }

    protected static async Task<GraphQLResult<string>> Register(
        ApiClient client,
        string name,
        string password = Password,
        bool asAdmin = false
    ) =>
        await client.Mutation(
            new
            {
                input = new RegisterInput
                {
                    Email = $"{name}@projectread.ing",
                    Password = password,
                    AdminRegistration = asAdmin,
                },
            },
            static (i, m) => m.RegisterMutation(i.input, p => p.Token)
        );

    protected static async Task<GraphQLResult<string>> Login(
        ApiClient client,
        string name,
        string password = Password
    ) =>
        await client.Mutation(
            new
            {
                input = new LoginInput
                {
                    Email = $"{name}@projectread.ing",
                    Password = password,
                },
            },
            static (i, m) => m.LoginMutation(i.input, p => p.Token)
        );

    protected static Guid NodeIdToGuid(ID id) =>
        Guid.TryParse(id.Value, out var plain)
            ? plain
            : throw new ArgumentException("cannot parse id", nameof(id));

    protected static async Task AssertErrorCode<T>(GraphQLResult<T> result, string code)
    {
        await Assert.That(result.Errors).IsNotNull().And.IsNotEmpty();
        var found = result.Errors!.Any(e =>
            (e.Message?.Contains(code, StringComparison.Ordinal) ?? false)
            || (
                e.Extensions?.Values.Any(v =>
                    v?.ToString()?.Contains(code, StringComparison.Ordinal) ?? false
                )
                ?? false
            )
        );
        await Assert.That(found).IsTrue();
    }

    protected static async Task<(Guid Id, int RowVersion)> AddAuthor(
        ApiClient client,
        string displayName = "Test Author",
        string? firstName = null,
        string? lastName = null,
        string[]? penNames = null
    )
    {
        var result = await client.Mutation(
            new
            {
                input = new AddAuthorInput
                {
                    DisplayName = displayName,
                    FirstName = firstName,
                    LastName = lastName,
                    PenNames = penNames ?? ["Pen Name"],
                },
            },
            static (i, m) =>
                m.AddAuthorMutation(
                    i.input,
                    p => p.Author(a => new { a.Id, a.RowVersion })
                )
        );
        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        return (NodeIdToGuid(result.Data!.Id), result.Data.RowVersion);
    }

    protected static async Task<(Guid Id, int RowVersion)> AddTag(
        ApiClient client,
        string tagName = "Fantasy",
        string[]? tagNamespace = null
    )
    {
        var result = await client.Mutation(
            new
            {
                input = new AddTagInput
                {
                    TagName = tagName,
                    TagNamespace = tagNamespace ?? ["genre"],
                },
            },
            static (i, m) =>
                m.AddTagMutation(i.input, p => p.Tag(t => new { t.Id, t.RowVersion }))
        );
        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        return (NodeIdToGuid(result.Data!.Id), result.Data.RowVersion);
    }

    protected static async Task<(Guid Id, int RowVersion)> AddWork(
        ApiClient client,
        string title = "Test Work",
        string? description = null,
        DateTimeOffset? publishedAt = null,
        DateTimeOffset? updatedAt = null,
        WorkIdentifierInput[]? identifiers = null,
        Guid[]? tagIds = null,
        Guid[]? authorIds = null
    )
    {
        var result = await client.Mutation(
            new
            {
                input = new AddWorkInput
                {
                    Title = title,
                    Description = description,
                    WorkPublishedAt = publishedAt,
                    WorkUpdatedAt = updatedAt,
                    WorkIdentifiers = identifiers ?? [],
                    TagIds = tagIds ?? [],
                    AuthorIds = authorIds ?? [],
                },
            },
            static (i, m) =>
                m.AddWorkMutation(i.input, p => p.Work(w => new { w.Id, w.RowVersion }))
        );
        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        return (NodeIdToGuid(result.Data!.Id), result.Data.RowVersion);
    }
}
// Mostly Ai Generated - End
