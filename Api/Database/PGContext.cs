// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Api.Database;

public partial class PGContext(DbContextOptions options, IConfiguration? config = null)
    : DbContext(options)
{
    public string SchemaName { get; set; } =
        config?["ConnectionStrings:PGSchema"] ?? "public";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetAssembly(this.GetType()!)!
        );
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder
            .ReplaceService<IModelCacheKeyFactory, SchemaModelCacheKeyFactory>()
            .UseSnakeCaseNamingConvention()
            .UseValidationCheckConstraints()
            .UseNpgsql(
                config?.GetConnectionString("PG") ?? "",
                nopts => nopts.SetPostgresVersion(18, 0).UseNodaTime()
            );
}

public class SchemaModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        return context is PGContext pgContext
            ? (context.GetType(), pgContext.SchemaName, designTime)
            : (context.GetType(), designTime);
    }
}
