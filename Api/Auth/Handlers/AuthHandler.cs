// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Buffers.Text;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using Geralt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Auth.Handlers;

class AuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    PGContext db
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string?[] tokens =
        [
            Context.Request.Headers.Authorization.FirstOrDefault(),
            Context.Request.Cookies[AuthUtils.TokenCookieName],
        ];
        var tokenHash = SelectToken(tokens);

        if (tokenHash is null)
        {
            return AuthenticateResult.NoResult();
        }

        var identity = await (
            tokenHash.Prefix switch
            {
                AuthUtils.UserTokenPrefixName => GetUserIdentityAsync(
                    tokenHash.TokenHash
                ),

                AuthUtils.RemoteServiceTokenPrefixName => Context
                    .Request.Headers[RemoteServiceToken.ServiceIdentifierType]
                    .First()
                    .TryParseGuid()
                    is { } id
                    ? GetServiceIdentity(tokenHash.TokenHash, id)
                    : Task.FromResult<ClaimsIdentity?>(null),
                _ => Task.FromResult<ClaimsIdentity?>(null),
            }
        );

        if (identity is not null)
        {
            return AuthenticateResult.Success(
                new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsPrincipal(identity)),
                    Scheme.Name
                )
            );
        }

        return AuthenticateResult.NoResult();
    }

    private record Token(byte[] TokenHash, string Prefix);

    private static Token? SelectToken(string?[] tokens)
    {
        var tokenHashes = tokens
            .Where(s =>
                s != null && AuthUtils.TokenPrefixesNames.Any(p => s.StartsWith(p))
            )
            .Select(s =>
            {
                if (s is null)
                {
                    return null;
                }
                var prefix = s.Split(AuthUtils.PrefixSeparator).First();
                var apiToken = Base64Url.DecodeFromChars(
                    s.AsSpan()[(prefix.Length + 1)..]
                );

                var tokenHash = new byte[32];
                BLAKE2b.ComputeHash(tokenHash, apiToken);
                return new Token(tokenHash, prefix);
            })
            .Where(t => t is not null)
            .ToArray();

        return tokenHashes.Length == 0 ? null : tokenHashes.First();
    }

    private async Task<ClaimsIdentity?> GetUserIdentityAsync(byte[] tokenHash)
    {
        var userToken = await db
            .UserTokens.AsNoTracking()
            .Where(ut => tokenHash.SequenceEqual(ut.TokenHash))
            .FirstOrDefaultAsync();

        if (userToken is null)
        {
            return null;
        }
        userToken = AuthUtils.UpdateLastUsedTokens(userToken);

        await db.SaveChangesAsync();

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userToken.UserId.ToString()),
            new Claim(
                UserTokenEF.PermissionBitsType,
                ((long)userToken.Permissions).ToString()
            ),
        ];
        return new ClaimsIdentity(claims, "user_token");
    }

    private async Task<ClaimsIdentity?> GetServiceIdentity(
        byte[] tokenHash,
        Guid targetUser
    )
    {
        var serviceToken = await db
            .RemoteServiceTokens.Where(t => t.TokenHash.SequenceEqual(tokenHash))
            .Include(t => t.RemoteService)
                .ThenInclude(t => t.UserPermissions.Where(u => u.UserId == targetUser))
            .FirstOrDefaultAsync();

        var userPermission = serviceToken?.RemoteService.UserPermissions.FirstOrDefault();

        if (serviceToken is null || userPermission is null)
        {
            return null;
        }

        AuthUtils.UpdateLastUsedTokens(serviceToken);
        AuthUtils.UpdateLastUsedTokens(userPermission);

        await db.SaveChangesAsync();

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userPermission.UserId.ToString()),
            new Claim(
                UserTokenEF.PermissionBitsType,
                ((long)userPermission.Permissions).ToString()
            ),
            new Claim(
                RemoteServiceToken.ServiceIdentifierType,
                serviceToken.RemoteServiceId.ToString()
            ),
        ];

        return new ClaimsIdentity(claims, "service_token");
    }
}
