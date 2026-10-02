// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Api.QueryTypes;

[OneOf]
public record ComperessionFilter<T>(
    EqFilter<T>? Eq,
    GtFilter<T>? Gt,
    GteFilter<T>? Gte,
    LtFilter<T>? Lt,
    LteFilter<T>? Lte
) : OneOfFilter<T>
    where T : IComparisonOperators<T, T, bool>
{
    public override Expression<Func<T, bool>> Filter =>
        throw new NotImplementedException();

    protected new virtual Expression<Func<T, bool>> GetFilter()
    {
        return (
                (
                    OneOfFilterSetup.FilterProperties?[GetType()]?[
                        SelectedProperty
                    ].GetValue(this)
                ) as IFilter<T>
            )?.Filter
            ?? base.GetFilter();
    }
}

public class EqFilter<T>(T other) : IFilter<T>
{
    public T Other { get; set; } = other;

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter
    {
        get
        {
            var p = Expression.Parameter(typeof(T), typeof(T).Name);
            var c = Expression.Constant(Other, typeof(T));
            return Expression.Lambda<Func<T, bool>>(Expression.Equal(p, c), p);
        }
    }
}

public class GtFilter<T>(T other) : IFilter<T>
{
    public T Other { get; set; } = other;

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter
    {
        get
        {
            var p = Expression.Parameter(typeof(T), typeof(T).Name);
            var c = Expression.Constant(Other, typeof(T));
            return Expression.Lambda<Func<T, bool>>(Expression.GreaterThan(p, c), p);
        }
    }
}

public class LtFilter<T>(T other) : IFilter<T>
{
    public T Other { get; set; } = other;

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter
    {
        get
        {
            var p = Expression.Parameter(typeof(T), typeof(T).Name);
            var c = Expression.Constant(Other, typeof(T));
            return Expression.Lambda<Func<T, bool>>(Expression.LessThan(p, c), p);
        }
    }
}

public class GteFilter<T>(T other) : IFilter<T>
    where T : IComparisonOperators<T, T, bool>
{
    public T Other { get; set; } = other;

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter
    {
        get
        {
            var p = Expression.Parameter(typeof(T), typeof(T).Name);
            var c = Expression.Constant(Other, typeof(T));
            return Expression.Lambda<Func<T, bool>>(
                Expression.GreaterThanOrEqual(p, c),
                p
            );
        }
    }
}

public class LteFilter<T>(T other) : IFilter<T>
{
    public T Other { get; set; } = other;

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter
    {
        get
        {
            var p = Expression.Parameter(typeof(T), typeof(T).Name);
            var c = Expression.Constant(Other, typeof(T));
            return Expression.Lambda<Func<T, bool>>(Expression.LessThanOrEqual(p, c), p);
        }
    }
}
