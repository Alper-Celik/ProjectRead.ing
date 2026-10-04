// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Api.QueryTypes;

[OneOf]
public record EqualityFilter<T> : OneOfFilter<T>
{
    public EqFilter<T>? Eq
    {
        get { return (EqFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public NeqFilter<T>? Neq
    {
        get { return (NeqFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }
}

[OneOf]
public record ComperessionFilter<T> : EqualityFilter<T>
{
    public GtFilter<T>? Gt
    {
        get { return (GtFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public GteFilter<T>? Gte
    {
        get { return (GteFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public LtFilter<T>? Lt
    {
        get { return (LtFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public LteFilter<T>? Lte
    {
        get { return (LteFilter<T>?)GetOneOf(); }
        set { SetOneOf(value); }
    }
}

public record EqFilter<T>(T Other) : IFilter<T>
{
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

public record NeqFilter<T>(T Other) : IFilter<T>
{
    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter
    {
        get
        {
            var p = Expression.Parameter(typeof(T), typeof(T).Name);
            var c = Expression.Constant(Other, typeof(T));
            return Expression.Lambda<Func<T, bool>>(Expression.NotEqual(p, c), p);
        }
    }
}

public record GtFilter<T>(T Other) : IFilter<T>
{
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

public record LtFilter<T>(T Other) : IFilter<T>
{
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

public record GteFilter<T>(T Other) : IFilter<T>
{
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

public record LteFilter<T>(T Other) : IFilter<T>
{
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

public record ContainsFilter(string Needle) : IFilter<string>
{
    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<string, bool>> Filter =>
        (x) => x != null && x.Contains(Needle);
}
