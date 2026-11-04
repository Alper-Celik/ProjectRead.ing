// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;

namespace Api.Database;

public partial class PGContext : DbContext
{
    public DbSet<Jobs.Models.JobData> JobDatas { get; set; }
}
