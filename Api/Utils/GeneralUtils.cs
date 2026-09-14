// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Immutable;
using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Utils;

public static class GeneralUtils
{
    // Mostly Ai Generated - Start
    public static IQueryable<T> WithQueryContext<T>(
        this IQueryable<T> query,
        QueryContext<T> qc
    )
        where T : IEntityMetadata =>
        query.With(
            qc with
            {
                Selector = null,
            },
            sort => sort.Operations.Length == 0 ? sort.AddAscending(t => t.Id) : sort
        );

    // Mostly Ai Generated - End
    public static async Task<Page<T>> ToPageWithDataLoaderAsync<T>(
        this IQueryable<T> table,
        PagingArguments pg,
        IDataLoader<Guid, T> dataLoader,
        CancellationToken ct
    )
        where T : IEntityMetadata
    {
        var page = await table.ToPageAsync(pg, includeTotalCount: true, ct);

        foreach (var item in page)
        {
            dataLoader.SetCacheEntry(item.Id, item);
        }
        return page;
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
