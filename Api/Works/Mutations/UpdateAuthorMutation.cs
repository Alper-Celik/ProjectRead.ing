// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using Api.Utils;
using Api.Works.Models;
using Api.Works.Queries;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Api.Works.Mutations;

[MutationType]
public static partial class UpdateAuthorMutations
{
    [PermissionCheckAuthorize(
        UserPermissionBits.AuthorRead | UserPermissionBits.AuthorWrite
    )]
    public static async Task<UpdateAuthorPayload> UpdateAuthorMutation(
        [Service] PGContext db,
        [Service] IEFTransactionDIAccessorService txGetter,
        UpdateAuthorInput input,
        CancellationToken ct
    )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync();
        var author = await db.Authors.SingleAsync(
            a => a.Id == input.Id,
            cancellationToken: ct
        );

        UpdateAuthorInput.ApplyToAuthor(author, input);
        author.RowVersion += 1;
        author.MetadataAddedAt = Now();

        await db.SaveChangesAsync(cancellationToken: ct);

        await tx.CommitAsync(ct);
        return new UpdateAuthorPayload(AuthorMapper.ToDto(author));
    }
}

public record UpdateAuthorPayload(Queries.Author Author);

public record UpdateAuthorInput : IBasicEntityMetadata
{
    public static byte IdPostfix => Models.Author.IdPostfix;

    public Guid Id { get; init; }
    public int RowVersion { get; init; }

    public Optional<string?> FirstName { get; init; }
    public Optional<string?> LastName { get; init; }
    public required Optional<string> DisplayName { get; init; }
    public required Optional<List<string>?> PenNames { get; init; }

    public class UpdateAuthorInputValidator : AbstractValidator<UpdateAuthorInput>
    {
        public UpdateAuthorInputValidator(
            PGContext db,
            ICurrentUserId userId,
            IEFTransactionDIAccessorService tx
        )
        {
            RuleFor(w => w)
                .MustAsync(
                    async (_, ct) =>
                    {
                        await tx.BeginOrGetTransactionAsync();
                        return true;
                    }
                );

            RuleFor(w => w.Id).IdMustExist(db.Authors, userId.Id);

            RuleFor(w => w.RowVersion).RowVersionMustMatch(db.Authors);
        }
    }

    public static void ApplyToAuthor(Models.Author author, UpdateAuthorInput input)
    {
        if (input.FirstName.HasValue)
            author.FirstName = input.FirstName.Value;

        if (input.LastName.HasValue)
            author.LastName = input.LastName.Value;

        if (input.DisplayName.HasValue)
            author.DisplayName = input.DisplayName.Value;

        if (input.PenNames.HasValue)
            author.PenNames = input.PenNames.Value ?? [];
    }
}
