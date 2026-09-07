// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Buffers.Text;

using Api.Auth.Models;
using Api.Database;

using Geralt;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using NodaTime;

namespace Api.Auth.Utils;

public static class LoginUtils
{

    public const char PrefixSeparator = '_';
    public const string UserTokenPrefixName = "user";
    public const string TokenCookieName = "auth_token";
    private static bool s_adminCreated = false;

    public static string UserTokenPrefix => UserTokenPrefixName + PrefixSeparator;


    public static async Task<string> CreateUserSession(Guid userId, string sessionName, PGContext ctx)
    {

        var apiToken = new byte[32];
        SecureRandom.Fill(apiToken);

        Span<byte> tokenHash = stackalloc byte[32];
        BLAKE2b.ComputeHash(tokenHash, apiToken);

        var dbToken = new UserTokenEF
        {
            UserId = userId,
            TokenHash = tokenHash.ToArray(),
            Permissions = UserPermissionBits.All,
            CreationTime = SystemClock.Instance.GetCurrentInstant(),
        };

        await ctx.UserTokens.AddAsync(dbToken);

        return UserTokenPrefix + Base64Url.EncodeToString(apiToken);
    }

    public static async Task<bool> CanAdminRegister(PGContext db)
    {
        if (s_adminCreated)
        {
            return false;
        }

        s_adminCreated = await db.Users.AnyAsync(u => u.Admin == true);
        return !s_adminCreated;
    }


    public static Ok<LoginResultDTO> LogUserIn(HttpContext ctx, string token)
    {
        ctx.Response.Cookies.Append(TokenCookieName, token, new CookieOptions
        {
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = ctx.Request.Scheme == "https",
            HttpOnly = true,
        });
        return TypedResults.Ok(new LoginResultDTO(token));
    }

    public static async Task UpdateLastUsedForUserToken(UserTokenEF token, PGContext ctx)
    {
        var currentTime = SystemClock.Instance.GetCurrentInstant();
        if (token.LastUsed is null ||
                token.LastUsed + Duration.FromMinutes(1) <= currentTime)
        {
            await ctx.UserTokens.Where(ut => ut.TokenHash.SequenceEqual(token.TokenHash))
                .ExecuteUpdateAsync(setter =>
                        setter.SetProperty(ut => ut.LastUsed, currentTime));
        }
    }

    public record LoginResultDTO(string AuthToken);
}