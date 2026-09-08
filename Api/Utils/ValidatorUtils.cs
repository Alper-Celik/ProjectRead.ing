// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Data;
using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Utils;

public static class ValidatorUtils
{
    public static IRuleBuilderOptions<T, TInner> WhenOptionalSet<T, TInner>(
        this IRuleBuilderOptions<T, TInner> rule,
        Func<T, IOptional> expr
    ) => rule.When((t) => expr(t).HasValue && expr(t).Value is not null);

    public static IRuleBuilderOptions<T, IEnumerable<TInner>> MustBeDistinct<T, TInner>(
        this IRuleBuilder<T, IEnumerable<TInner>> rule,
        string propName,
        IEqualityComparer<TInner>? comparer = null
    )
    {
        return rule.Must(t => t.Distinct().SequenceEqual(t, comparer))
            .WithMessage($"{propName} must be distinct")
            .WithErrorCode(ErrorCodes.IS_NOT_DISTINCT);
    }

    public static IRuleBuilderOptions<T, TProp> IdsMustExist<T, TProp, TTarget>(
        this IRuleBuilder<T, TProp> rule,
        DbSet<TTarget> target,
        Guid? ownerId
    )
        where TProp : IEnumerable<Guid>
        where TTarget : class, IDbEntityMetadata
    {
        return rule.MustAsync(
                async (t, ids, ct) =>
                {
                    var q = target.Select(t => t).Where(t => ids.Contains(t.Id));

                    if (ownerId is not null)
                    {
                        q = q.Where(t => t.OwnerId == ownerId);
                    }

                    return (
                        await q.Select(t => t.Id).Distinct().Order().ToArrayAsync()
                    ).SequenceEqual(ids.Distinct().Order());
                }
            )
            .WithMessage($"{nameof(TTarget)} ids must exist")
            .WithErrorCode(ErrorCodes.IDS_DOES_NOT_EXIST);
    }

    public static IRuleBuilderOptions<T, Guid> IdMustExist<T, TTarget>(
        this IRuleBuilder<T, Guid> rule,
        DbSet<TTarget> target,
        Guid? ownerId
    )
        where TTarget : class, IDbEntityMetadata
    {
        return rule.MustAsync(
                async (id, ct) =>
                {
                    var q = target.Where(t => t.Id == id);
                    if (ownerId is not null)
                    {
                        q = q.Where(t => t.OwnerId == ownerId);
                    }
                    return await q.AnyAsync(ct);
                }
            )
            .WithMessage($"{nameof(TTarget)} id must exist")
            .WithErrorCode(ErrorCodes.ID_DOES_NOT_EXIST);
    }

    public static IRuleBuilderOptions<T, int> RowVersionMustMatch<T, TTarget>(
        this IRuleBuilder<T, int> rule,
        DbSet<TTarget> target
    )
        where T : class, IBasicEntityMetadata
        where TTarget : class, IDbEntityMetadata
    {
        return rule.MustAsync(
                async (t, rowVer, ct) =>
                    await target
                        .Where(tt => tt.Id == t.Id && tt.RowVersion == rowVer)
                        .AnyAsync(cancellationToken: ct)
            )
            .WithMessage(
                $"Row version for `{nameof(TTarget)}` does not match with provided row version"
            )
            .WithErrorCode(ErrorCodes.ROW_VERSION_MISMATCH);
    }
}
