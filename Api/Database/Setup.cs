// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Database.Utils;

namespace Api.Database;

public static class Setup
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<
            IEFTransactionDIAccessorService,
            EFTransactionDIAccessorService
        >();
        services.AddDbContext<PGContext>();
    }
}
