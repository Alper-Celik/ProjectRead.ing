// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Api.Auth.Utils;
using Api.Database;
using Geralt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

public class AuthTests : TestInit
{
    [Test]
    public async Task OnlyFirstAccountCanBeAdmin(CancellationToken ct)
    {
        var preRegister_bool = await CanRegisterAdmin(Client);
        var user1 = await AddUser(Client, "didnt_wanted_to_be_admin", false);
        var postNonAdminRegister_bool = await CanRegisterAdmin(Client);
        var adminUser = await AddUser(Client, "sysadmin", true);
        var postAdminRegister_bool = await CanRegisterAdmin(Client);
        var failedRegisterUser = await AddUser(Client, "want_poweeer", true);

        preRegister_bool.HttpResponseMessage.EnsureSuccessStatusCode();
        await Assert.That(preRegister_bool.Data).IsTrue();

        await Assert.That(user1.Errors).IsNull().Or.IsEmpty();

        postAdminRegister_bool.HttpResponseMessage.EnsureSuccessStatusCode();
        await Assert.That(postNonAdminRegister_bool.Data).IsTrue();

        await Assert.That(adminUser.Errors).IsNull().Or.IsEmpty();

        await Assert.That(postAdminRegister_bool.Data).IsFalse();

        failedRegisterUser.HttpResponseMessage.EnsureSuccessStatusCode(); //even in failure it should return successful graphql response
        await Assert.That(failedRegisterUser.Errors).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task CantLoginWithWrongPassword(CancellationToken ct)
    {
        var user = await AddUser(Client, "user", false, "hunter2");

        var loginSuccess = await Login(Client, "user", "hunter2");

        var loginFail = await Login(Client, "user", "*******");

        await Assert
            .That(loginSuccess.Data)
            .IsNotNullOrEmpty()
            .And.StartsWith(Auth.Utils.AuthUtils.UserTokenPrefixName);

        await Assert.That(loginFail.Data).IsNullOrEmpty();
    }

    // Mostly Ai Generated - Start
    [Test]
    public async Task NonAdminRegistrations_AreUnlimited_EvenAfterAdminExists(
        CancellationToken ct
    )
    {
        await AddUser(Client, "first_admin", true);

        var users = new[]
        {
            AddUser(Client, "reader_1", false),
            AddUser(Client, "reader_2", false),
            AddUser(Client, "reader_3", false),
        };
        await Task.WhenAll(users);

        foreach (var result in users.Select(u => u.Result))
        {
            result.HttpResponseMessage.EnsureSuccessStatusCode();
            await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        }
    }

    // Mostly Ai Generated - End

    // Mostly Ai Generated - Start
    [Test]
    public async Task AuthenticatingWithUserToken_UpdatesLastUsedTimestamp(
        CancellationToken ct
    )
    {
        var http = Factory.CreateClient();
        http.BaseAddress = new Uri(http.BaseAddress!.AbsoluteUri + "graphql/");
        var client = new ApiClient(http);

        var registered = await client.Mutation(
            new
            {
                input = new RegisterInput()
                {
                    AdminRegistration = false,
                    Email = "last_used@projectread.ing",
                    Password = "correct horse battery staple",
                },
            },
            static (i, m) => m.RegisterMutation(i.input, p => p.Token)
        );
        await Assert.That(registered.Errors).IsNull().Or.IsEmpty();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            registered.Data!
        );

        var tokenHash = UserTokenHash(registered.Data!);
        await Assert.That(await LastUsedOf(tokenHash, ct)).IsNull();

        var me = await client.Query(q => q.CurrentUser(u => new { u.Email }));
        await Assert.That(me.Errors).IsNull().Or.IsEmpty();

        await Assert.That(await LastUsedOf(tokenHash, ct)).IsNotNull();
    }

    private static byte[] UserTokenHash(string userToken)
    {
        var apiToken = Base64Url.DecodeFromChars(
            userToken.AsSpan()[(AuthUtils.UserTokenPrefixName.Length + 1)..]
        );
        var tokenHash = new byte[32];
        BLAKE2b.ComputeHash(tokenHash, apiToken);
        return tokenHash;
    }

    private async Task<Instant?> LastUsedOf(byte[] tokenHash, CancellationToken ct)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PGContext>();
        return (
            await db
                .UserTokens.AsNoTracking()
                .SingleAsync(t => t.TokenHash.SequenceEqual(tokenHash), ct)
        ).LastUsed;
    }

    // Mostly Ai Generated - End

    private static async Task<ZeroQL.GraphQLResult<DateTimeOffset>> AddUser(
        ApiClient client,
        string name,
        bool asAdmin,
        string? password = null
    )
    {
        var input = new
        {
            input = new RegisterInput()
            {
                AdminRegistration = asAdmin,
                Email = $"{name}@projectread.ing",
                Password = password ?? "correct horse battery staple",
            },
        };
        return await client.Mutation(
            input,
            static (i, m) =>
                m.RegisterMutation(i.input, m => m.User(u => u.MetadataAddedAt))
        );
    }

    private static Task<ZeroQL.GraphQLResult<bool>> CanRegisterAdmin(ApiClient client) =>
        client.Query(q => q.RegisterInfo(r => r.CanRegisterAsAdmin));

    private static async Task<GraphQLResult<string>> Login(
        ApiClient client,
        string name,
        string password
    ) =>
        await client.Mutation(
            new
            {
                input = new LoginInput()
                {
                    Email = $"{name}@projectread.ing",
                    Password = password,
                },
            },
            static (i, m) => m.LoginMutation(i.input, lm => lm.Token)
        );
}
