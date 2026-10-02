// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Api.QueryTypes;

public interface IOneOfFilterMarker;

public abstract record OneOfFilter<T> : IOneOfFilterMarker
{
    protected OneOfFilter()
    {
        OneOfFilterSetup.ReflectProperties();

        var properties =
            OneOfFilterSetup
                .FilterProperties?[GetType()].Where(p =>
                    p.Value.GetValue(this) is not null
                )
                .ToArray()
            ?? [];

        if (properties.Length != 1)
        {
            throw new ArgumentException();
        }

        SelectedProperty = properties[0].Key;
    }

    [JsonIgnore]
    [GraphQLIgnore]
    public string SelectedProperty { get; init; }

    [GraphQLIgnore]
    protected virtual Expression<Func<T, bool>> GetFilter() => (_) => true;

    [JsonIgnore]
    [GraphQLIgnore]
    public abstract Expression<Func<T, bool>> Filter { get; }
}

public static class OneOfFilterSetup
{
    public static void ReflectProperties()
    {
        if (FilterProperties is not null)
            return;
        var oneOfTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                (
                    !t.IsGenericType
                    || t.GetGenericTypeDefinition() != typeof(OneOfFilter<>)
                ) && t.IsAssignableTo(typeof(IOneOfFilterMarker))
            );

        FilterProperties = oneOfTypes.ToDictionary(
            t => t,
            t =>
                t.GetProperties()
                    .Where(p =>
                        p.GetGetMethod()
                            ?.ReturnType.GetInterfaces()
                            .Any(i =>
                                i.IsGenericType
                                && i.GetGenericTypeDefinition() == typeof(IFilter<>)
                            )
                        ?? false
                    )
                    .ToDictionary(p => p.Name)
        );
    }

    public static Dictionary<Type, Dictionary<string, PropertyInfo>>? FilterProperties;
}
