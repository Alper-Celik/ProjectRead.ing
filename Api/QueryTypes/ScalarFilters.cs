// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NodaTime;

namespace Api.QueryTypes;

[OneOf]
public record GuidFilter() : OneOfFilter<Guid>
{
    public EqFilter<Guid>? Eq
    {
        get { return (EqFilter<Guid>?)GetOneOf(); }
        set { SetOneOf(value); }
    }
}

[OneOf]
public record ITimeFilter : ComperessionFilter<Instant>;
