// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using System.Text.Json;
using Api.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;
using ZeroQL.Client;

namespace Api.Tests;

public class MyWebApplicationFactory : TestWebApplicationFactory<Program> { }

public abstract class TestInit : WebApplicationTest<MyWebApplicationFactory, Program>
{
    public ApiClient Client
    {
        get
        {
            if (field is null)
            {
                var httpClient = Factory.CreateClient();
                httpClient.BaseAddress = new Uri(
                    httpClient.BaseAddress!.AbsoluteUri + "graphql/"
                );
                field = new ApiClient(httpClient);
            }
            return field;
        }
        set;
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config)
    {
        Stream strStream = new MemoryStream(
            Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(
                    new
                    {
                        IsTest = true,
                        ConnectionStrings = new
                        {
                            PGSchema = GetIsolatedName("test_schema"),
                            PG = new Npgsql.NpgsqlConnectionStringBuilder(
                                config.Build().GetConnectionString("PG")
                            )
                            {
                                Database = DBInfo.Name,
                            }.ConnectionString,
                        },
                    }
                )
            )
        );
        config.AddJsonStream(strStream);
    }

    [ClassDataSource<DBData>(Shared = SharedType.PerTestSession)]
    public required DBData DBInfo { get; init; }

    public record DBData : IAsyncInitializer, IAsyncDisposable
    {
        public string Name { get; init; } = $"test_db";
        readonly Npgsql.NpgsqlDataSource _npgsqlDataSource =
            new Npgsql.NpgsqlDataSourceBuilder(
                $"Host=localhost;Username=postgres"
            ).Build();

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await _npgsqlDataSource.DisposeAsync();
        }

        public async Task InitializeAsync()
        {
            await using (
                var cmd_drop = _npgsqlDataSource.CreateCommand(
                    $"DROP DATABASE IF EXISTS \"{Name}\""
                )
            )
            {
                await cmd_drop.ExecuteNonQueryAsync();
            }
            await using var cmd = _npgsqlDataSource.CreateCommand(
                $"CREATE DATABASE \"{Name}\""
            );
            await cmd.ExecuteScalarAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        {
            await using var scope = Factory.Services.CreateAsyncScope();
            await using var dbConn = scope.ServiceProvider.GetService<PGContext>()!;
            await dbConn.GetService<IRelationalDatabaseCreator>().DeleteAsync();
        }

        await Factory.DisposeAsync();
    }
}
