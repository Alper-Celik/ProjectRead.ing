// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;

public class GuidNodeSerializer : INodeIdSerializer
{

    public Dictionary<byte, string> PostfixTypeNames { get; init; }
    public Dictionary<Type, byte> TypePostFix { get; init; }

    public MethodInfo GetSuffixGeneric { get; init; }

    static byte GetSuffix<T>() where T : IEntityMetadata => T.IdPostfix;
    public GuidNodeSerializer()
    {


        var nodeTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t =>
                t.GetInterfaces()
                    .Where(
                        t => (new Type[] { typeof(INode), typeof(IEntityMetadata) })
                        .Contains(t))
                    .Distinct().Count() == 2).ToArray();


        GetSuffixGeneric = typeof(GuidNodeSerializer).GetMethod(nameof(GetSuffix), BindingFlags.NonPublic | BindingFlags.Static)!;

        TypePostFix = nodeTypes
           .Select(t => new KeyValuePair<Type, byte>(
                       t,
                       (byte)GetSuffixGeneric!.MakeGenericMethod(t).Invoke(null, null)!
                       )).ToDictionary();
        PostfixTypeNames = TypePostFix.Select(kv => new KeyValuePair<byte, string>(kv.Value, kv.Key.Name)).ToDictionary();
    }

    public string Format(string typeName, object internalId)
        => internalId is Guid id
            ? id.ToString("D")
            : throw new ArgumentException();

    public NodeId Parse(string formattedId, INodeIdRuntimeTypeLookup runtimeTypeLookup) => Parse(formattedId);

    public NodeId Parse(string formattedId, Type runtimeType) => Parse(formattedId);

    public NodeId Parse(string formattedId)
    {
        var id = Guid.Parse(formattedId);
        var idPostfix = id.ToByteArray().Last();
        if (PostfixTypeNames.TryGetValue(idPostfix, out var typeName))
        {
            return new(typeName, id);
        }
        throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Invalid Id")
                .SetCode(Api.ErrorCodes.INVALID_ID).Build());
    }


}