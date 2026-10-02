// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using NodaTime;

namespace Api.QueryTypes;

[OneOf]
public record GuidFilter : EqualityFilter<Guid>;

[OneOf]
public record RowVersionFilter : EqualityFilter<int>;

[OneOf]
public record TimeFilter : ComperessionFilter<Instant>;
