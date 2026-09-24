// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Api.Auth.Models;

[PrimaryKey(nameof(RemoteServiceId), nameof(UserId))]
public class RemoteServiceUserPermission : ITokenTime
{
    [ForeignKey(nameof(RemoteService))]
    public Guid RemoteServiceId { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    public required UserPermissionBits Permissions { get; set; }

    public Instant CreationTime { get; set; }

    public Instant? LastUsed { get; set; }

    // Navigation Properties
    public RemoteService RemoteService { get; set; } = null;
    public UserEF User { get; set; } = null!;
}
