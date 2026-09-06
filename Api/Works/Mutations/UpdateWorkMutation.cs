// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later


using Api.Auth.Utils;
using Api.Database;
using Api.Database.Utils;
using Api.Utils;

using FairyBread;

using FluentValidation;

using NodaTime;

namespace Api.Works.Mutations;

[MutationType]
public static partial class UpdateWorkMutations
{
    public static async Task<UpdateWorkPayload> UpdateWorkMutation(
            [Service] PGContext db,
            [Service] ICurrentUserId userId,
            [Service] IEFTransactionDIAccessorService txGetter,
            CancellationToken ct,
            UpdateWorkInput input
            )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync();
        db.Works.Find()

        await tx.CommitAsync();
    }
}

public record UpdateWorkPayload(
        Queries.Work Work
        );

public record UpdateWorkInput : IBasicEntityMetadata
{
    public static byte IdPostfix => Models.Work.IdPostfix;

    public Guid Id { get; init; }
    public int RowVersion { get; init; }

    [DefaultValue("")]
    public required Optional<string> Title { get; init; }
    public Optional<string?> Description { get; init; }

    public Optional<Instant?> WorkPublishedAt { get; init; }
    public Optional<Instant?> WorkUpdatedAt { get; init; }
    public Optional<List<Queries.Work.WorkIdentifier>?> WorkIdentifiers { get; init; }
    public required Optional<List<Guid>?> TagIds { get; init; }
    public required Optional<List<Guid>?> AuthorIds { get; init; }

    public class UpdateWorkInputValidator : AbstractValidator<UpdateWorkInput>
    {
        public UpdateWorkInputValidator(PGContext db, ICurrentUserId userId, IEFTransactionDIAccessorService tx)
        {
            RuleFor(w => w).MustAsync(async (_, ct) => { await tx.BeginOrGetTransactionAsync(); return true; });

            RuleFor(w => w.Id).IdMustExist(db.Works, userId.Id);

            RuleFor(w => w.RowVersion).RowVersionMustMatch(db.Works);

            RuleFor(w => w.TagIds.Value!).MustBeDistinct(nameof(TagIds)).WhenOptionalSet(w => w.TagIds);
            RuleFor(w => w.TagIds.Value!).IdsMustExist(db.WorkTags, userId.Id).WhenOptionalSet(w => w.TagIds);


            RuleFor(w => w.AuthorIds.Value!).MustBeDistinct(nameof(AuthorIds)).WhenOptionalSet(w => w.AuthorIds);
            RuleFor(w => w.AuthorIds.Value!).IdsMustExist(db.Authors, userId.Id).WhenOptionalSet(w => w.AuthorIds);
        }
    }


}
