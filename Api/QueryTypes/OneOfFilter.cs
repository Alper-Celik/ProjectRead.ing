// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using LinqKit;

namespace Api.QueryTypes;

public interface IOneOfFilterMarker;

public abstract record OneOfFilter<T> : IFilter<T>, IOneOfFilterMarker
{
    protected object? GetOneOf([CallerMemberName] string propName = "") =>
        SelectedPropertyName == propName ? SelectedProperty : null;

    protected void SetOneOf<U>(
        IFilter<U>? value,
        Expression<Func<T, U>> selector,
        [CallerMemberName] string propName = ""
    )
    {
        if (value is not null)
        {
            SelectedPropertyName = propName;
            SelectedProperty = value;
            Filter = (t) => value.Filter.Invoke(selector.Invoke(t));
        }
    }

    protected void SetOneOf<U>(U? value, [CallerMemberName] string propName = "")
        where U : IFilter<T>
    {
        if (value is not null)
        {
            SelectedPropertyName = propName;
            SelectedProperty = value;
            Filter = value.Filter;
        }
    }

    [JsonIgnore]
    [GraphQLIgnore]
    public string SelectedPropertyName { get; protected set; } = string.Empty;

    [JsonIgnore]
    [GraphQLIgnore]
    public object SelectedProperty { get; protected set; } = string.Empty;

    [JsonIgnore]
    [GraphQLIgnore]
    public Expression<Func<T, bool>> Filter { get; protected set; } = null!;
}
