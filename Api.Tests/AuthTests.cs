// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;


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
        await Assert.That(failedRegisterUser.Errors)
            .IsNotNull()
            .And.IsNotEmpty();
    }

    [Test]
    public async Task CantLoginWithWrongPassword(CancellationToken ct)
    {
        var user = await AddUser(Client, "user", false, "hunter2");

        var loginSuccess = await Login(Client, "user", "hunter2");

        var loginFail = await Login(Client, "user", "*******");

        await Assert.That(loginSuccess.Data).IsNotNullOrEmpty()
            .And.StartsWith(Auth.Utils.LoginUtils.UserTokenPrefixName);
        await Assert.That(loginFail.Data).IsNullOrEmpty();

    }

    private static async Task<ZeroQL.GraphQLResult<DateTimeOffset>> AddUser(ApiClient client, string name, bool asAdmin, string? password = null)
    {
        var input = new
        {
            input = new RegisterInput()
            {
                AdminRegistration = asAdmin,
                Email = $"{name}@projectread.ing",
                Password = password ?? "correct horse battery staple"
            }
        };
        return await client.Mutation(input, static (i, m) => m.RegisterMutation(i.input, m => m.User(u => u.MetadataAddedAt)));
    }

    private static Task<ZeroQL.GraphQLResult<bool>> CanRegisterAdmin(ApiClient client) => client.Query(q => q.RegisterInfo(r => r.CanRegisterAsAdmin));

    private static async Task<GraphQLResult<string>> Login(ApiClient client, string name, string password) => await client.Mutation(new
    {
        input = new LoginInput()
        {
            Email = $"{name}@projectread.ing",
            Password = password
        }
    }, static (i, m) => m.LoginMutation(i.input, lm => lm.Token));



}