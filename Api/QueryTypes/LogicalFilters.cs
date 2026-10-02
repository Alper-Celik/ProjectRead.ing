// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using LinqKit;

namespace Api.QueryTypes;

public class AndFilter<T>(IEnumerable<IFilter<T>> filters) : IFilter<T>
{
    public Expression<Func<T, bool>> Filter =>
        filters
            .Select(f => f.Filter)
            .Aggregate((a, b) => (t) => a.Invoke(t) && b.Invoke(t));
}

public class OrFilter<T>(IEnumerable<IFilter<T>> filters) : IFilter<T>
{
    public Expression<Func<T, bool>> Filter =>
        filters
            .Select(f => f.Filter)
            .Aggregate((a, b) => (t) => a.Invoke(t) || b.Invoke(t));
}
