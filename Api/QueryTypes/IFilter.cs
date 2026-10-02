// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;

namespace Api.QueryTypes;

public interface IFilter<T>
{
    public Expression<Func<T, bool>> Filter { get; }
}
