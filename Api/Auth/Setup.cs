// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
using Api.Auth.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth;

public static class Setup
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUserId, CurrentUserId>();
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    }

    public static void MapEndpoints(IEndpointRouteBuilder route) { }
}
