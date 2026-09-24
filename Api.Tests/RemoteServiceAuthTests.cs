// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Mostly Ai Generated - Start
using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Utils;
using Geralt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using ZeroQL;
using ZeroQL.Client;

namespace Api.Tests;

/// <summary>
/// Integration tests for the remote service infra:
/// <see cref="Api.Auth.Mutations.LoginInstanceServiceMutations"/> (login),
/// <see cref="AuthHandler"/> (service token delegation) and the REST files
/// endpoints accessed through a delegated service identity.
/// </summary>
public class RemoteServiceAuthTests : FilesTestBase
{
    // must be valid Base64Url; hex chars are a subset of the Base64Url alphabet
    private const string Seed =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        base.ConfigureTestConfiguration(config);
        // test-only seed so the client side of the proof doesn't depend on the
        // dev secret in appsettings; added after base providers so it wins
        config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ProjectReadingSecretSeed"] = Seed }
        );
    }

    // Login mutation

    [Test]
    public async Task LoginInstanceService_WithValidProof_ReturnsServiceTokenAndPersistsToken(
        CancellationToken ct
    )
    {
        var (client, _) = AnonymousClients();
        var salt = RandomSalt();
        var second = NowUnixSeconds();

        var result = await LoginInstanceService(
            client,
            DeriveProof(salt, second),
            salt,
            second
        );

        await Assert.That(result.Errors).IsNull().Or.IsEmpty();
        await Assert
            .That(result.Data)
            .IsNotNull()
            .And.StartsWith(AuthUtils.RemoteServiceTokenPrefix);

        // the returned token must actually authenticate as the instance service
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PGContext>();
        var tokenHash = RemoteServiceTokenHash(result.Data!);
        var stored = await db.RemoteServiceTokens.SingleOrDefaultAsync(
            t => t.TokenHash.SequenceEqual(tokenHash),
            cancellationToken: ct
        );
        await Assert.That(stored).IsNotNull();
        await Assert
            .That(stored!.RemoteServiceId)
            .IsEqualTo(RemoteService.TheInstanceServiceId);
    }

    [Test]
    public async Task LoginInstanceService_WithTamperedProof_ReturnsInvalidCredentials(
        CancellationToken ct
    )
    {
        var (client, _) = AnonymousClients();
        var salt = RandomSalt();
        var second = NowUnixSeconds();
        var proof = DeriveProof(salt, second);
        // flip a byte in the middle of the proof; keeps length and Base64Url validity
        var chars = proof.ToCharArray();
        chars[60] = chars[60] == 'A' ? 'B' : 'A';

        var result = await LoginInstanceService(client, new string(chars), salt, second);
        await AssertErrorCode(result, Api.ErrorCodes.INVALID_CREDS);
    }

    [Test]
    public async Task LoginInstanceService_WithExpiredTimestamp_FailsValidation(
        CancellationToken ct
    )
    {
        var (client, _) = AnonymousClients();
        var salt = RandomSalt();
        var stale = NowUnixSeconds() - 600;

        var result = await LoginInstanceService(
            client,
            DeriveProof(salt, stale),
            salt,
            stale
        );

        await Assert.That(result.Errors).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task LoginInstanceService_WithShortSalt_FailsValidation(
        CancellationToken ct
    )
    {
        var (client, _) = AnonymousClients();
        var salt = "abc";
        var second = NowUnixSeconds();

        var result = await LoginInstanceService(
            client,
            DeriveProof(salt, second),
            salt,
            second
        );

        await Assert.That(result.Errors).IsNotNull().And.IsNotEmpty();
    }

    // Service token delegation (AuthHandler)

    [Test]
    public async Task ServiceToken_ActsOnBehalfOfGrantedUser(CancellationToken ct)
    {
        var (userClient, _, userId) = await RegisterUser("svc_target");
        await AddWork(userClient, title: "Target Work");
        await GrantServiceUser(userId, UserPermissionBits.All);
        var serviceToken = await MintServiceToken();

        var svc = ServiceClient(serviceToken, userId);

        var me = await svc.Query(q => q.CurrentUser(u => new { u.Email }));
        await Assert.That(me.Errors).IsNull().Or.IsEmpty();
        await Assert.That(me.Data!.Email).IsEqualTo("svc_target@projectread.ing");

        var works = await svc.Query(q =>
            q.Works(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: null,
                order: null,
                selector: c => c.Nodes(w => new { w.Title })
            )
        );
        await Assert.That(works.Errors).IsNull().Or.IsEmpty();
        await Assert
            .That(works.Data!.Select(w => w.Title))
            .IsEquivalentTo(["Target Work"]);
    }

    [Test]
    public async Task ServiceToken_OnlyGrantedPermissionBitsApply(CancellationToken ct)
    {
        var (userClient, _, userId) = await RegisterUser("svc_limited");
        await AddWork(userClient, title: "Limited Work");
        // WorkRead (1 << 0) but no UserRead (1 << 6)
        await GrantServiceUser(userId, UserPermissionBits.WorkRead);
        var serviceToken = await MintServiceToken();

        var svc = ServiceClient(serviceToken, userId);

        var works = await svc.Query(q =>
            q.Works(
                first: 10,
                after: null,
                last: null,
                before: null,
                where: null,
                order: null,
                selector: c => c.Nodes(w => new { w.Title })
            )
        );
        await Assert.That(works.Errors).IsNull().Or.IsEmpty();
        await Assert
            .That(works.Data!.Select(w => w.Title))
            .IsEquivalentTo(["Limited Work"]);

        var me = await svc.Query(q => q.CurrentUser(u => new { u.Email }));
        await Assert.That(me.Errors).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task ServiceToken_WithoutTargetUserHeader_IsRejected(
        CancellationToken ct
    )
    {
        var (_, _, userId) = await RegisterUser("svc_no_header");
        await GrantServiceUser(userId, UserPermissionBits.All);
        var serviceToken = await MintServiceToken();

        var svc = ServiceClient(serviceToken);

        var me = await svc.Query(q => q.CurrentUser(u => new { u.Email }));
        await Assert.That(me.Errors).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task ServiceToken_ForUserWithoutGrant_IsRejected(CancellationToken ct)
    {
        var (_, _, userId) = await RegisterUser("svc_ungranted");
        var serviceToken = await MintServiceToken();

        var svc = ServiceClient(serviceToken, userId);

        var me = await svc.Query(q => q.CurrentUser(u => new { u.Email }));
        await Assert.That(me.Errors).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task ServiceToken_UnknownToken_IsRejected(CancellationToken ct)
    {
        var (_, _, userId) = await RegisterUser("svc_unknown_token");

        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var svc = ServiceClient(
            AuthUtils.RemoteServiceTokenPrefix + Base64Url.EncodeToString(bytes),
            userId
        );

        var me = await svc.Query(q => q.CurrentUser(u => new { u.Email }));
        await Assert.That(me.Errors).IsNotNull().And.IsNotEmpty();
    }

    // REST endpoints through a delegated identity

    [Test]
    public async Task ServiceToken_RestFilesEndpoint_ServesGrantedUsersFile(
        CancellationToken ct
    )
    {
        var (userClient, userHttp, userId) = await RegisterUser("svc_file_owner");
        await GrantServiceUser(userId, UserPermissionBits.All);
        var serviceToken = await MintServiceToken();

        var recordId = await AddFileRecordAsync(userClient, FileRecordInput());
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(SampleContent);
        file.Headers.ContentType = new MediaTypeHeaderValue(SampleContentType);
        form.Add(file, "file", "book.epub");
        using var upload = await userHttp.PostAsync($"api/files/{recordId}", form, ct);
        await Assert.That(upload.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var svcHttp = Factory.CreateClient();
        svcHttp.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization",
            serviceToken
        );
        svcHttp.DefaultRequestHeaders.Add(
            RemoteServiceToken.ServiceIdentifierType,
            userId.ToString()
        );

        using var read = await svcHttp.GetAsync($"api/files/{recordId}", ct);

        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert
            .That(await read.Content.ReadAsByteArrayAsync(ct))
            .IsEquivalentTo(SampleContent);
    }

    [Test]
    public async Task ServiceToken_RestFilesEndpoint_WithoutGrant_ReturnsUnauthorized(
        CancellationToken ct
    )
    {
        var (userClient, _, userId) = await RegisterUser("svc_rest_ungranted");
        var recordId = await AddFileRecordAsync(userClient, FileRecordInput());
        var serviceToken = await MintServiceToken();

        var svcHttp = Factory.CreateClient();
        svcHttp.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization",
            serviceToken
        );
        svcHttp.DefaultRequestHeaders.Add(
            RemoteServiceToken.ServiceIdentifierType,
            userId.ToString()
        );

        using var read = await svcHttp.GetAsync($"api/files/{recordId}", ct);

        await Assert.That(read.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    // Helpers

    private (ApiClient Client, HttpClient Http) AnonymousClients()
    {
        var http = Factory.CreateClient();
        var graphqlHttp = Factory.CreateClient();
        graphqlHttp.BaseAddress = new Uri(
            graphqlHttp.BaseAddress!.AbsoluteUri + "graphql/"
        );
        return (new ApiClient(graphqlHttp), http);
    }

    /// <summary>Registers a user and returns an authenticated client plus the user id.</summary>
    private async Task<(ApiClient Client, HttpClient Http, Guid UserId)> RegisterUser(
        string name
    )
    {
        var graphqlHttp = Factory.CreateClient();
        graphqlHttp.BaseAddress = new Uri(
            graphqlHttp.BaseAddress!.AbsoluteUri + "graphql/"
        );
        var client = new ApiClient(graphqlHttp);

        var result = await Register(client, name);
        result.HttpResponseMessage.EnsureSuccessStatusCode();
        await Assert.That(result.Errors).IsNull().Or.IsEmpty();

        var authorization = new AuthenticationHeaderValue(result.Data!);
        graphqlHttp.DefaultRequestHeaders.Authorization = authorization;

        var http = Factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = authorization;

        var me = await client.Query(q => q.CurrentUser(u => new { u.Id }));
        await Assert.That(me.Errors).IsNull().Or.IsEmpty();
        return (client, http, NodeIdToGuid(me.Data!.Id));
    }

    private ApiClient ServiceClient(string serviceToken, Guid? targetUser = null)
    {
        var graphqlHttp = Factory.CreateClient();
        graphqlHttp.BaseAddress = new Uri(
            graphqlHttp.BaseAddress!.AbsoluteUri + "graphql/"
        );
        graphqlHttp.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization",
            serviceToken
        );
        if (targetUser is { } user)
        {
            graphqlHttp.DefaultRequestHeaders.Add(
                RemoteServiceToken.ServiceIdentifierType,
                user.ToString()
            );
        }
        return new ApiClient(graphqlHttp);
    }

    private async Task<string> MintServiceToken()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PGContext>();
        string token = await AuthUtils.CreateRemoteServiceSession(
            RemoteService.TheInstanceServiceId,
            db
        );
        await db.SaveChangesAsync();
        return token;
    }

    private async Task GrantServiceUser(Guid userId, UserPermissionBits permissions)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PGContext>();
        db.RemoteServiceUserPermissions.Add(
            new RemoteServiceUserPermission
            {
                RemoteServiceId = RemoteService.TheInstanceServiceId,
                UserId = userId,
                Permissions = permissions,
                CreationTime = GeneralUtils.Now(),
            }
        );
        await db.SaveChangesAsync();
    }

    private static Task<GraphQLResult<string>> LoginInstanceService(
        ApiClient client,
        string proof,
        string salt,
        long currentSecond
    ) =>
        client.Mutation(
            new
            {
                input = new LoginInstanceServiceInput
                {
                    AuthProof = proof,
                    Salt = salt,
                    CurrentSecond = currentSecond,
                },
            },
            static (i, m) => m.LoginTheInstanceService(i.input, p => p.Token)
        );

    /// <summary>
    /// Unix seconds (GraphQL Long): integer serialization is exact through any
    /// client, and the server derives the proof from the raw value, so the
    /// proof and the payload must share this one number.
    /// </summary>
    private static long NowUnixSeconds() =>
        SystemClock.Instance.GetCurrentInstant().ToUnixTimeSeconds();

    private static string RandomSalt()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return Base64Url.EncodeToString(salt);
    }

    private static string DeriveProof(string salt, long currentSecond) =>
        Base64Url.EncodeToString(
            HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                Base64Url.DecodeFromChars(Seed),
                720 / 8,
                Base64Url.DecodeFromChars(salt),
                Encoding.UTF8.GetBytes("The Instance Service Auth : " + currentSecond)
            )
        );

    private static byte[] RemoteServiceTokenHash(string serviceToken)
    {
        var apiToken = Base64Url.DecodeFromChars(
            serviceToken.AsSpan()[(AuthUtils.RemoteServiceTokenPrefixName.Length + 1)..]);
        var tokenHash = new byte[32];
        BLAKE2b.ComputeHash(tokenHash, apiToken);
        return tokenHash;
    }
}
// Mostly Ai Generated - End
