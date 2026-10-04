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

public record HasFilter<T, U>(IFilter<U> ElementFilter) : IFilter<T>
    where T : IHasConnection<T, U>
{
    [JsonIgnore]
    [GraphQLIgnore]
    private static readonly Expression<Func<T, IList<U>>> Selector =
        (Expression<Func<T, IList<U>>>)T.Connections[typeof(U)];

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter =>
        (t) => Selector.Invoke(t).Any(u => ElementFilter.Filter.Invoke(u));
}

public record HasNotFilter<T, U>(IFilter<U> ElementFilter) : IFilter<T>
    where T : IHasConnection<T, U>
{
    [JsonIgnore]
    [GraphQLIgnore]
    private static readonly Expression<Func<T, IList<U>>> Selector =
        (Expression<Func<T, IList<U>>>)T.Connections[typeof(U)];

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter =>
        (t) => !Selector.Invoke(t).Any(u => ElementFilter.Filter.Invoke(u));
}

public record ConnectionFilter<T, U> : OneOfFilter<T>
    where T : IHasConnection<T, U>
{
    public HasFilter<T, U>? Has
    {
        get { return (HasFilter<T, U>?)GetOneOf(); }
        set { SetOneOf(value); }
    }

    public HasNotFilter<T, U>? HasNot
    {
        get { return (HasNotFilter<T, U>?)GetOneOf(); }
        set { SetOneOf(value); }
    }
}
