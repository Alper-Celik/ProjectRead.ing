// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

namespace Api.Auth.Utils;

public interface ICurrentUserId
{
    public Guid? Id { get; }
}

public class CurrentUserId : ICurrentUserId
{
    public Guid? Id { get; init; }

    public CurrentUserId(IHttpContextAccessor ctx)
    {
        if (
            ctx.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                is { } userIdStr
            && Guid.TryParse(userIdStr, out Guid userId)
        )
        {
            Id = userId;
        }
    }
}
