// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Models;
using Api.Files.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Database;

public partial class PGContext : DbContext
{
    public DbSet<FileRecord> FileRecords { get; set; }

    public DbSet<FileProviderBackend> FileProviderBackends { get; set; }
}
