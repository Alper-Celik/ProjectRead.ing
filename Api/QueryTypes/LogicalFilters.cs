// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using LinqKit;

namespace Api.QueryTypes;

[OneOf]
public record LogicalFilter<T>() : OneOfFilter<T>
{
    public AndFilter<T>? And
    {
        get { return (AndFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public OrFilter<T>? Or
    {
        get { return (OrFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public IFilter<T>? Just
    {
        get { return (IFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }
}

public record AndFilter<T>(IEnumerable<IFilter<T>> Filters) : IFilter<T>
{
    public Expression<Func<T, bool>> Filter =>
        Filters
            .Select(f => f.Filter)
            .Aggregate((a, b) => (t) => a.Invoke(t) && b.Invoke(t));
}

public record OrFilter<T>(IEnumerable<IFilter<T>> Filters) : IFilter<T>
{
    public Expression<Func<T, bool>> Filter =>
        Filters
            .Select(f => f.Filter)
            .Aggregate((a, b) => (t) => a.Invoke(t) || b.Invoke(t));
}
