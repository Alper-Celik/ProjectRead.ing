// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Database;
using HotChocolate;
using Npgsql;

namespace Api.Utils;

public static class DbExceptionUtils
{
    /// <summary>
    /// Saves changes; when PostgreSQL reports an error with <paramref name="sqlState"/>
    /// (<see langword="null"/> matches any PostgreSQL error), throws a
    /// <see cref="GraphQLException"/> tagged with <paramref name="code"/>/<paramref name="message"/>
    /// instead of the raw database error.
    /// </summary>
    public static async Task SaveChangesOrThrowAsync(
        this PGContext db,
        string? sqlState,
        string code,
        string message,
        CancellationToken ct
    )
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken: ct);
        }
        catch (Exception ex) when (TryGetPostgresException(ex, out var pg))
        {
            if (sqlState is not null && sqlState != pg.SqlState)
            {
                throw;
            }

            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(message).SetCode(code).Build()
            );
        }
    }

    /// <summary>
    /// Unwraps the PostgreSQL error behind an EF Core save failure, if there is one.
    /// </summary>
    public static bool TryGetPostgresException(Exception ex, out PostgresException pg)
    {
        while (true)
        {
            if (ex is PostgresException postgres)
            {
                pg = postgres;
                return true;
            }

            if (ex.InnerException is null)
            {
                pg = null!;
                return false;
            }

            ex = ex.InnerException;
        }
    }
}
