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

    // Mostly Ai Generated - Start
    // Official tag namespace separator: `::` is forbidden inside tag namespace
    // components and tag names, so the joined form round-trips losslessly.
    // A single `:` is allowed and never ambiguous.
    public const string TagNamespaceSeparator = "::";

    // A tag namespace component or tag name must be non-empty, contain no `::`
    // separator, and not start/end with `:` — otherwise the joined full-name form
    // cannot be split back losslessly. A single `:` in the middle is fine.
    public static bool IsValidTagComponent(string? s) =>
        s is not null
        && s.Length > 0
        && !s.Contains(TagNamespaceSeparator)
        && !s.StartsWith(':')
        && !s.EndsWith(':');

    public static string ToTagFullName(string[] tagNamespace, string tagName) =>
        string.Join(TagNamespaceSeparator, [.. tagNamespace, tagName]);

    public static string[] FromTagFullName(string tagFullName) =>
        tagFullName.Split(TagNamespaceSeparator, StringSplitOptions.None);

    // Mostly Ai Generated - End

    public static string NormalizeEmail(this string email) =>
        email.Trim().Normalize().ToLowerInvariant();

    public static NodaTime.Instant Now() =>
        NodaTime.SystemClock.Instance.GetCurrentInstant();

    public static Guid? TryParseGuid(this ReadOnlySpan<char> s) =>
        Guid.TryParse(s, out Guid g) ? g : null;

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
