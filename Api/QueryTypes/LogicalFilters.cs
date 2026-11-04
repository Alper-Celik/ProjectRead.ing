// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using LinqKit;

namespace Api.QueryTypes;

public record LogicalFilter<T, FilterType, LogicalFilterType> : OneOfFilter<T>
    where FilterType : IFilter<T>
    where LogicalFilterType : LogicalFilter<T, FilterType, LogicalFilterType>, IFilter<T>
{
    public AndFilter<T, LogicalFilterType>? And
    {
        get { return (AndFilter<T, LogicalFilterType>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public OrFilter<T, LogicalFilterType>? Or
    {
        get { return (OrFilter<T, LogicalFilterType>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public FilterType? Just
    {
        get { return (FilterType?)GetOneOf(); }
        set { SetOneOf(value); }
    }
}

public record AndFilter<T, LogicalFilterType>(IEnumerable<LogicalFilterType> Filters)
    : IFilter<T>
    where LogicalFilterType : IFilter<T>
{
    public Expression<Func<T, bool>> Filter =>
        Filters
            .Select(f => f.Filter)
            .Aggregate((a, b) => (t) => a.Invoke(t) && b.Invoke(t));
}

public record OrFilter<T, LogicalFilterType>(IEnumerable<LogicalFilterType> Filters)
    : IFilter<T>
    where LogicalFilterType : IFilter<T>
{
    public Expression<Func<T, bool>> Filter =>
        Filters
            .Select(f => f.Filter)
            .Aggregate((a, b) => (t) => a.Invoke(t) || b.Invoke(t));
}
