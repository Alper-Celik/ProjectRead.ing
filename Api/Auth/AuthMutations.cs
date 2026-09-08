// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Unicode;

using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;

using FairyBread;

using FluentValidation;

using Geralt;

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

using static Api.Auth.AuthMutationsUtils;
using static Api.Auth.AuthQueriesUtils;

namespace Api.Auth;

[MutationType]
public static partial class AuthMutations
{

    [AllowAnonymous]
    public static async Task<LoginPayload> LoginMutation(
            [Service] PGContext db,
            [Service] IEFTransactionDIAccessorService txAccessor,
            LoginInput input)
    {
        var tx = await txAccessor.BeginOrGetTransactionAsync();
        UserEF? user = await db.Users.Where(u => u.Email == input.Email.NormalizeEmail())
           .FirstOrDefaultAsync();

        var junkHash = new char[Argon2id.HashSize];
        Argon2id.ComputeHash(junkHash, Encoding.UTF8.GetBytes("123"), ARGON2ID_ITER, ARGON2ID_MEM_BYTES);

        var passworkdVerified = Argon2id.VerifyHash(user?.PasswordHash ?? new string(junkHash), Encoding.UTF8.GetBytes(input.Password.Normalize()));

        if (user is not null && passworkdVerified)
        {
            var token = await LoginUtils.CreateUserSession(user.Id, input.ClientName, db);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return new(UserMapper.ToDto(user), token);
        }

        throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Invalid Credentials")
                .SetCode(ErrorCodes.INVALID_CREDS)
                .Build());
    }

    [AllowAnonymous]
    public static async Task<LoginPayload> RegisterMutation(
            [Service] PGContext db,
            [Service] IEFTransactionDIAccessorService txAccessor,
            RegisterInput input)
    {
        var tx = await txAccessor.BeginOrGetTransactionAsync();

        var password_bytes = Encoding.UTF8.GetBytes(input.Password.Normalize());
        var hash_chars = new char[Argon2id.HashSize];
        Argon2id.ComputeHash(hash_chars, password_bytes, ARGON2ID_ITER, ARGON2ID_MEM_BYTES);
        string hash = new([.. hash_chars.Where(c => c != (char)byte.MinValue)]);

        var now = Now();
        var user = new UserEF()
        {
            Id = Guid.CreateVersion7().WithPostfix(UserEF.IdPostfix),
            MetadataAddedAt = now,
            MetadataUpdatedAt = now,

            Email = input.Email.NormalizeEmail(),
            PasswordHash = hash,
            Admin = (await LoginUtils.CanAdminRegister(db)) && input.AdminRegistration
        };

        await db.Users.AddAsync(user);
        string token = await LoginUtils.CreateUserSession(user.Id, input.ClientName, db);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new(UserMapper.ToDto(user), token);
    }
}

public static class AuthMutationsUtils
{

    // see https://www.rfc-editor.org/rfc/rfc9106.html#name-recommendations
    public const int ARGON2ID_ITER = 3;
    public const int ARGON2ID_MEM_BYTES = 64 * 1024 * 1024;


    public record LoginPayload(
            User User,
            string Token
            );
    public record LoginInput(
            string Email,
            string Password,
            string ClientName = "unknown"
            );

    public record RegisterInput(
            string Email,
            string Password,
            bool AdminRegistration,
            string ClientName = "unknown"
            );
    public class RegisterInputValidator : AbstractValidator<RegisterInput>, IRequiresOwnScopeValidator
    {
        public RegisterInputValidator(PGContext db, IEFTransactionDIAccessorService tx)
        {
            RuleFor(w => w).MustAsync(async (_, ct) => { await tx.BeginOrGetTransactionAsync(); return true; });

            RuleFor(r => r.Email)
                .Must(e => new EmailAddressAttribute().IsValid(e.NormalizeEmail()))
                .WithMessage("Email is invalid");

            RuleFor(r => r.Email)
                .MustAsync(async (e, ct) =>
                        !await db.Users
                        .Where(u => u.Email.NormalizeEmail() == e)
                        .AnyAsync(ct))
                .WithMessage("Email is already used");


            RuleFor(r => r.AdminRegistration)
                .MustAsync(async (adr, ct) =>
                        !adr || await LoginUtils.CanAdminRegister(db))
                .WithMessage("Can't Register As Admin");
        }
    }
}