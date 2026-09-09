// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Immutable;
using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Utils;

public static class GeneralUtils
{
    public static async Task<Page<T>> ToPageWithDataLoaderAsync<T>(
        this IQueryable<T> table,
        PagingArguments pg,
        IDataLoader<Guid, T> dataLoader,
        CancellationToken ct
    )
        where T : IEntityMetadata
    {
        var ids = await table.Select(t => t.Id).ToPageAsync(pg, ct);

        var contents = await dataLoader.LoadAsync([.. ids], cancellationToken: ct)!;

        return ReplaceIdPage<T>(ImmutableArray.Create<T>([.. contents!]), ids!);
    }

    public static Page<T> ReplaceIdPage<T>(this T[] items, Page<Guid> page)
        where T : IEntityMetadata => ImmutableArray.Create(items).ReplaceIdPage(page);

    public static Page<T> ReplaceIdPage<T>(this ImmutableArray<T> items, Page<Guid> page)
        where T : IEntityMetadata
    {
        var pageEntries = page.Entries.ToDictionary(p => p.Item);

        return Page<T>.Create(
            items: items,
            hasNextPage: page.HasNextPage,
            hasPreviousPage: page.HasPreviousPage,
            createCursor: (T t) => page.CreateCursor(pageEntries[t.Id]),
            totalCount: page.TotalCount
        );
    }

    public static string NormalizeEmail(this string email) =>
        email.Trim().Normalize().ToLowerInvariant();

    public static NodaTime.Instant Now() =>
        NodaTime.SystemClock.Instance.GetCurrentInstant();
}
