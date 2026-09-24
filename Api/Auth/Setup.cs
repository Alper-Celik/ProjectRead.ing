// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Api.Auth.Handlers;
using Api.Auth.Models;
using Api.Auth.Utils;
using Api.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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

    public static async Task SeedDb(PGContext db, CancellationToken ct)
    {
        var instanceService = await db.RemoteServices.FindAsync(
            [RemoteService.TheInstanceServiceId],
            cancellationToken: ct
        );

        if (instanceService is null)
        {
            var now = Now();
            instanceService = new RemoteService()
            {
                Id = RemoteService.TheInstanceServiceId,
                MetadataAddedAt = now,
                MetadataUpdatedAt = now,
                IsInstanceService = true,
                DefaultInstanceWidePermisssion = UserPermissionBits.All,
                Name = RemoteService.TheInstanceServiceName,
            };
            await db.RemoteServices.AddAsync(instanceService, ct);
            await db.SaveChangesAsync(ct);
        }
        else if (
            instanceService.IsInstanceService != true
            || instanceService.Name != RemoteService.TheInstanceServiceName
            || instanceService.DefaultInstanceWidePermisssion != UserPermissionBits.All
        )
        {
            instanceService.Name = RemoteService.TheInstanceServiceName;
            instanceService.IsInstanceService = true;
            instanceService.DefaultInstanceWidePermisssion = UserPermissionBits.All;
            instanceService.MetadataUpdatedAt = Now();

            await db.SaveChangesAsync(ct);
        }
    }
}
