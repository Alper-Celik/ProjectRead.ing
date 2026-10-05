// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Api.Utils;
using LinqKit;

namespace Api.QueryTypes;

public interface IHasConnection<T, U>
{
    public static abstract IReadOnlyDictionary<
        Type,
        LambdaExpression
    > Connections { get; }
}

public record HasFilter<FromType, ToType, FilterType>(FilterType ElementFilter)
    : IFilter<FromType>
    where FilterType : IFilter<ToType>
    where FromType : IHasConnection<FromType, ToType>
{
    [JsonIgnore]
    [GraphQLIgnore]
    private static readonly Expression<Func<FromType, IList<ToType>>> Selector =
        (Expression<Func<FromType, IList<ToType>>>)FromType.Connections[typeof(ToType)];

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<FromType, bool>> Filter =>
        (t) => Selector.Invoke(t).Any(u => ElementFilter.Filter.Invoke(u));
}

public record HasNotFilter<FromType, ToType, FilterType>(FilterType ElementFilter)
    : IFilter<FromType>
    where FilterType : IFilter<ToType>
    where FromType : IHasConnection<FromType, ToType>
{
    [JsonIgnore]
    [GraphQLIgnore]
    private static readonly Expression<Func<FromType, IList<ToType>>> Selector =
        (Expression<Func<FromType, IList<ToType>>>)FromType.Connections[typeof(ToType)];

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<FromType, bool>> Filter =>
        (t) => !Selector.Invoke(t).Any(u => ElementFilter.Filter.Invoke(u));
}

public record ConnectionFilter<FromType, ToType, FilterType> : OneOfFilter<FromType>
    where FilterType : IFilter<ToType>
    where FromType : IHasConnection<FromType, ToType>
{
    public HasFilter<FromType, ToType, FilterType>? Has
    {
        get { return (HasFilter<FromType, ToType, FilterType>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public HasNotFilter<FromType, ToType, FilterType>? HasNot
    {
        get { return (HasNotFilter<FromType, ToType, FilterType>?)GetOneOf(); }
        set { SetOneOf(value); }
    }
}
