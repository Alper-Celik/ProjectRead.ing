// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq.Expressions;
using NodaTime;

namespace Api.Auth.Models;

public interface ITokenTime
{
    public Instant CreationTime { get; set; }
    public Instant? LastUsed { get; set; }
}
