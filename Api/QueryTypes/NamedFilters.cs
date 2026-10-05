// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Works.Models;
using NodaTime;

namespace Api.QueryTypes;

[OneOf]
public record GuidFilter : EqualityFilter<Guid>;

[OneOf]
public record RowVersionFilter : EqualityFilter<int>;

[OneOf]
public record TimeFilter : ComperessionFilter<Instant?>;

[OneOf]
public record BasicStringFilter : EqualityFilter<string?>
{
    public ContainsFilter? Contains
    {
        get { return (ContainsFilter?)GetOneOf(); }
        set { SetOneOf(value); }
    }
}

public record WorkToAuthorConnectionFilter : ConnectionFilter<Work, Author, AuthorFilter>;
