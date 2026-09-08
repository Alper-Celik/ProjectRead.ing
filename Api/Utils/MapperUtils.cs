// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NodaTime;

using Riok.Mapperly.Abstractions;

namespace Api.Utils;

[Mapper]
public static partial class MapperUtils
{
    [UserMapping(Default = true)]
    public static ZonedDateTime FromInstantToZonedDateTime(Instant i) => i.InUtc();
}