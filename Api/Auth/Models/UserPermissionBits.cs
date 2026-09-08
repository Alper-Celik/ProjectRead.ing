// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Api.Auth.Models;

[Flags]
public enum UserPermissionBits : long
{
    All = ~0,
    WorkRead = 1L << 0,
    WorkWrite = 1L << 1,

    AuthorRead = 1L << 2,
    AuthorWrite = 1L << 3,

    WorkTagRead = 1L << 4,
    WorkTagWrite = 1L << 5,

    UserRead = 1L << 6,
    UserWrite = 1L << 7,
}
