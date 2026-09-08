// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Utils;

namespace Api.Auth;

public static class Setup
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUserId, CurrentUserId>();
        services.AddHttpContextAccessor();
    }

    public static void MapEndpoints(IEndpointRouteBuilder route)
    {

    }
}