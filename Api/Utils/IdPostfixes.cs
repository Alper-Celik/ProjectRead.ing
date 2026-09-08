// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

public enum IdPostfixes : byte
{
    User = 0,
    Work = 1,
    Author = 2,
    WorkTag = 3,
}

public static class GuidExtensions
{
    public static Guid WithPostfix(this Guid id, IdPostfixes postfix) => WithPostfix(id, (byte)postfix);
    public static Guid WithPostfix(this Guid id, byte postfix)
    {
        Span<byte> newGuid = stackalloc byte[16];
        id.TryWriteBytes(newGuid);
        newGuid[15] = postfix;
        return new Guid(newGuid);
    }
}