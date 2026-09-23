// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
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

public record RegisterInput(
    string Email,
    string Password,
    bool AdminRegistration,
    string ClientName = "unknown"
);

[MutationType]
public static partial class RegisterMutations
{
    [AllowAnonymous]
    public static async Task<LoginPayload> RegisterMutation(
        [Service] PGContext db,
        [Service] IEFTransactionDIAccessorService txAccessor,
        RegisterInput input
    )
    {
        var tx = await txAccessor.BeginOrGetTransactionAsync();

        var password_bytes = Encoding.UTF8.GetBytes(input.Password.Normalize());
        var hash_chars = new char[Argon2id.HashSize];
        Argon2id.ComputeHash(
            hash_chars,
            password_bytes,
            AuthUtils.ARGON2ID_ITER,
            AuthUtils.ARGON2ID_MEM_BYTES
        );
        string hash = new([.. hash_chars.Where(c => c != (char)byte.MinValue)]);

        var now = Now();
        var user = new UserEF()
        {
            Id = Guid.CreateVersion7().WithPostfix(UserEF.IdPostfix),
            MetadataAddedAt = now,
            MetadataUpdatedAt = now,

            Email = input.Email.NormalizeEmail(),
            PasswordHash = hash,
            Admin = (await AuthUtils.CanAdminRegister(db)) && input.AdminRegistration,
        };

        await db.Users.AddAsync(user);
        string token = await AuthUtils.CreateUserSession(user.Id, input.ClientName, db);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return new(Queries.UserMapper.ToDto(user), token);
    }
}

public class RegisterInputValidator : AbstractValidator<RegisterInput>
{
    public RegisterInputValidator(PGContext db, IEFTransactionDIAccessorService tx)
    {
        RuleFor(w => w)
            .MustAsync(
                async (_, ct) =>
                {
                    await tx.BeginOrGetTransactionAsync();
                    return true;
                }
            );

        RuleFor(r => r.Email)
            .Must(e => new EmailAddressAttribute().IsValid(e.NormalizeEmail()))
            .WithMessage("Email is invalid");

        RuleFor(r => r.Email)
            .MustAsync(
                async (e, ct) =>
                    !await db.Users.Where(u => u.Email == e.NormalizeEmail()).AnyAsync(ct)
            )
            .WithMessage("Email is already used");

        RuleFor(r => r.AdminRegistration)
            .MustAsync(async (adr, ct) => !adr || await AuthUtils.CanAdminRegister(db))
            .WithMessage("Can't Register As Admin");
    }
}
