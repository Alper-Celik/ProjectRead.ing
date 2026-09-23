// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using FluentValidation;
using Geralt;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Api.Auth.Mutations;

public record LoginPayload(Queries.User User, string Token);

public record LoginInput(string Email, string Password, string ClientName = "unknown");

[MutationType]
public static partial class LoginMutations
{
    [AllowAnonymous]
    public static async Task<LoginPayload> LoginMutation(
        [Service] PGContext db,
        [Service] IEFTransactionDIAccessorService txAccessor,
        LoginInput input
    )
    {
        var tx = await txAccessor.BeginOrGetTransactionAsync();
        UserEF? user = await db
            .Users.Where(u => u.Email == input.Email.NormalizeEmail())
            .FirstOrDefaultAsync();

        var junkHash = new char[Argon2id.HashSize];
        Argon2id.ComputeHash(
            junkHash,
            Encoding.UTF8.GetBytes("123"),
            AuthUtils.ARGON2ID_ITER,
            AuthUtils.ARGON2ID_MEM_BYTES
        );

        var passworkdVerified = Argon2id.VerifyHash(
            user?.PasswordHash ?? new string(junkHash),
            Encoding.UTF8.GetBytes(input.Password.Normalize())
        );

        if (user is not null && passworkdVerified)
        {
            var token = await AuthUtils.CreateUserSession(user.Id, input.ClientName, db);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return new(Queries.UserMapper.ToDto(user), token);
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
