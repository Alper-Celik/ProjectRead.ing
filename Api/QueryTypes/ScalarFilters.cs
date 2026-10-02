// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using NodaTime;

namespace Api.QueryTypes;

[OneOf]
public record GuidFilter(EqFilter<Guid> Eq);

// public class ITimeFilter(
