// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Linq.Expressions;
using System.Text.Json;
using GreenDonut.Data;
using LinqKit;
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

    public static async Task<IDictionary<Guid, Guid[]>> GetManyToManyIds<T>(
        this IQueryable<T> table,
        IReadOnlyList<Guid> tableIds,
        Guid userId,
        Expression<Func<T, IEnumerable<Guid>>> manyIdSelector,
        CancellationToken ct
    )
        where T : IDbEntityMetadata
    {
        return await table
            .Where(t => t.OwnerId == userId)
            .Where(t => tableIds.Contains(t.Id))
            .Select(t => new { t.Id, ManyIds = manyIdSelector.Invoke(t) })
            .ToDictionaryAsync(ids => ids.Id, ids => ids.ManyIds.ToArray(), ct);
    }

    public static string NormalizeEmail(this string email) =>
        email.Trim().Normalize().ToLowerInvariant();

    public static NodaTime.Instant Now() =>
        NodaTime.SystemClock.Instance.GetCurrentInstant();

    public static Guid? TryParseGuid(this ReadOnlySpan<char> s) =>
        Guid.TryParse(s, out Guid g) ? null : g;

    public static T? TryDeserialize<T>(
        this JsonDocument doc,
        JsonSerializerOptions? opt = null
    )
        where T : class
    {
        try
        {
            return doc.Deserialize<T>(opt);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
