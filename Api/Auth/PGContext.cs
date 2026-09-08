// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Models;

using Microsoft.EntityFrameworkCore;

namespace Api.Database;

public partial class PGContext : DbContext
{
    public DbSet<UserEF> Users { get; set; }

    public DbSet<UserTokenEF> UserTokens { get; set; }
}