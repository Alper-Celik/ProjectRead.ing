// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

using Api.Auth.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Api.Auth.Handlers;

public class PermissionCheckRequirement(UserPermissionBits permissionBits) : IAuthorizationRequirement
{
    public UserPermissionBits PermissionBits { get; } = permissionBits;
}
public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string POLICY_PREFIX = "PermissionBits_";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(POLICY_PREFIX) &&
                long.TryParse(policyName[POLICY_PREFIX.Length..], out long permissionBits))
        {
            return Task.FromResult<AuthorizationPolicy?>(
                    new AuthorizationPolicyBuilder()
                            .AddRequirements(
                                new PermissionCheckRequirement(
                                    (UserPermissionBits)permissionBits))
                            .Build());
        }

        return base.GetPolicyAsync(policyName);
    }
}





class PermissionCheckAuthorizationHandler : AuthorizationHandler<PermissionCheckRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionCheckRequirement requirement)
    {
        var userPermissionBitsString = context.User.FindFirstValue(UserTokenEF.PermissionBitsType);
        if (!long.TryParse(userPermissionBitsString, out long userPermissionBitsLong))
        {
            return Task.CompletedTask;
        }
        var userPermissionBits = (UserPermissionBits)userPermissionBitsLong;

        if ((requirement.PermissionBits & userPermissionBits) == requirement.PermissionBits)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}



class PermissionCheckAuthorizeAttribute
    : HotChocolate.Authorization.AuthorizeAttribute
    , IAuthorizeData
{
    public PermissionCheckAuthorizeAttribute(UserPermissionBits permissionBits)
    {
        Policy = $"{PermissionPolicyProvider.POLICY_PREFIX}{(long)permissionBits}";
    }

    public string? AuthenticationSchemes { get; set; }
    string? IAuthorizeData.Roles { get; set; }
}