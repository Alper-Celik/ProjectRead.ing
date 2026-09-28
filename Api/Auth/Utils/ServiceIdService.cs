// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Api.Auth.Models;

namespace Api.Auth.Utils;

public interface ICurrentServiceId
{
    public Guid? Id { get; }
}

public class CurrentServiceId : ICurrentServiceId
{
    public Guid? Id { get; init; }

    public CurrentServiceId(IHttpContextAccessor ctx)
    {
        if (
            ctx.HttpContext?.User.FindFirstValue(RemoteServiceToken.ServiceIdentifierType)
                is { } serviceIdStr
            && Guid.TryParse(serviceIdStr, out Guid serviceId)
        )
        {
            Id = serviceId;
        }
    }
}
