// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Buffers.Text;
using Api.Auth.Models;
using Api.Database;
using Api.Database.Utils;
using Geralt;
using LinqKit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Api.Auth.Utils;

public static class AuthUtils
{
    public const char PrefixSeparator = '_';

    public const string UserTokenPrefixName = "user";
    public const string RemoteServiceTokenPrefixName = "service";
    public static List<string> TokenPrefixesNames =>
        [UserTokenPrefixName, RemoteServiceTokenPrefixName];
    public const string TokenCookieName = "auth_token";

    public static string UserTokenPrefix => UserTokenPrefixName + PrefixSeparator;
    public static string RemoteServiceTokenPrefix =>
        RemoteServiceTokenPrefixName + PrefixSeparator;

    // see https://www.rfc-editor.org/rfc/rfc9106.html#name-recommendations
    public const int ARGON2ID_ITER = 3;
    public const int ARGON2ID_MEM_BYTES = 64 * 1024 * 1024;

    public static async Task<string> CreateRemoteServiceSession(
        Guid serviceId,
        PGContext db
    )
    {
        byte[] apiToken = new byte[32];
        SecureRandom.Fill(apiToken);

        byte[] tokenHash = new byte[32];
        BLAKE2b.ComputeHash(tokenHash, apiToken);

        var serviceToken = new RemoteServiceToken
        {
            RemoteServiceId = serviceId,
            TokenHash = tokenHash,
            CreationTime = Now(),
        };

        await db.RemoteServiceTokens.AddAsync(serviceToken);

        return RemoteServiceTokenPrefix + Base64Url.EncodeToString(apiToken);
    }

    public static async Task<string> CreateUserSession(
        Guid userId,
        string sessionName,
        PGContext ctx
    )
    {
        var apiToken = new byte[32];
        SecureRandom.Fill(apiToken);

        byte[] tokenHash = new byte[32];
        BLAKE2b.ComputeHash(tokenHash, apiToken);

        var dbToken = new UserTokenEF
        {
            UserId = userId,
            SessionName = sessionName,
            TokenHash = tokenHash,
            Permissions = UserPermissionBits.All,
            CreationTime = Now(),
        };

        await ctx.UserTokens.AddAsync(dbToken);

        return UserTokenPrefix + Base64Url.EncodeToString(apiToken);
    }

    public static async Task<bool> CanAdminRegister(PGContext db) =>
        !await db.Users.AnyAsync(u => u.Admin == true);

    public static Ok<LoginResultDTO> LogUserIn(HttpContext ctx, string token)
    {
        ctx.Response.Cookies.Append(
            TokenCookieName,
            token,
            new CookieOptions
            {
                IsEssential = true,
                SameSite = SameSiteMode.Strict,
                Secure = ctx.Request.Scheme == "https",
                HttpOnly = true,
            }
        );
        return TypedResults.Ok(new LoginResultDTO(token));
    }

    public static T UpdateLastUsedTokens<T>(T token)
        where T : class, ITokenTime
    {
        var currentTime = SystemClock.Instance.GetCurrentInstant();
        if (
            token.LastUsed is null
            || token.LastUsed + Duration.FromMinutes(10) <= currentTime
        )
        {
            token.LastUsed = currentTime;
        }
        return token;
    }

    public record LoginResultDTO(string AuthToken);
}
