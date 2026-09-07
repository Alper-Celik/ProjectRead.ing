// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Buffers.Text;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Auth.Handlers;

class AuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, PGContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string?[] tokenHashes = [Context.Request.Headers.Authorization.LastOrDefault(), Context.Request.Cookies[LoginUtils.TokenCookieName]];
        List<byte[]> userTokenHashes = [.. tokenHashes
            .Where(s => s != null && s.StartsWith(LoginUtils.UserTokenPrefix))
        .Select(s => Base64Url.DecodeFromChars(
                    s.AsSpan()[LoginUtils.UserTokenPrefix.Length ..]))];

        var userToken = await db.UserTokens.Where(ut => userTokenHashes.Contains(ut.TokenHash)).FirstOrDefaultAsync();

        if (userToken is not null)
        {
            await LoginUtils.UpdateLastUsedForUserToken(userToken, db);


            Claim[] claims = [
                new Claim(ClaimTypes.NameIdentifier,userToken.UserId.ToString()),
                new Claim(UserTokenEF.PermissionBitsType,((long)userToken.Permissions).ToString()),
            ];

            var identity = new ClaimsIdentity(claims, "user_token");

            return AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new ClaimsPrincipal(
                                new ClaimsPrincipal(identity)
                            ),
                        Scheme.Name)
                    );
        }


        return AuthenticateResult.NoResult();
    }

}