// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using FluentValidation;
using HotChocolate.Authorization;
using NodaTime;

namespace Api.Auth.Mutations;

/// <param name="CurrentSecond">Unix timestamp in seconds (GraphQL Long).</param>
public record LoginInstanceServiceInput(
    string AuthProof,
    string Salt,
    long CurrentSecond
);

public record LoginInstanceServicePayload(string Token);

[MutationType]
public static partial class LoginInstanceServiceMutations
{
    [AllowAnonymous]
    public static async Task<LoginInstanceServicePayload> LoginTheInstanceService(
        [Service] PGContext db,
        [Service] IEFTransactionDIAccessorService txAccessor,
        [Service] IConfiguration config,
        LoginInstanceServiceInput input,
        CancellationToken ct
    )
    {
        var proof = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            Base64Url.DecodeFromChars(config["ProjectReadingSecretSeed"]),
            720 / 8,
            Base64Url.DecodeFromChars(input.Salt),
            Encoding.UTF8.GetBytes("The Instance Service Auth : " + input.CurrentSecond)
        );

        if (Geralt.ConstantTime.Equals(proof, Base64Url.DecodeFromChars(input.AuthProof)))
        {
            string token = await AuthUtils.CreateRemoteServiceSession(
                RemoteService.TheInstanceServiceId,
                db
            );
            await db.SaveChangesAsync(ct);

            await txAccessor.CommitTX(ct);
            return new(token);
        }

        throw new GraphQLException(
            ErrorBuilder
                .New()
                .SetMessage("Invalid Credentials")
                .SetCode(ErrorCodes.INVALID_CREDS)
                .Build()
        );
    }
}

public class LoginInstanceServiceInputValidator
    : AbstractValidator<LoginInstanceServiceInput>
{
    public LoginInstanceServiceInputValidator()
    {
        RuleFor(i => i.AuthProof).Must(str => Base64Url.IsValid(str)).Length(120);

        RuleFor(i => i.Salt).Must(str => Base64Url.IsValid(str)).MinimumLength(22);

        RuleFor(i => i.CurrentSecond)
            .LessThan((_) => Now().Plus(Duration.FromMinutes(5)).ToUnixTimeSeconds())
            .GreaterThan((_) => Now().Minus(Duration.FromMinutes(5)).ToUnixTimeSeconds());
    }
}
