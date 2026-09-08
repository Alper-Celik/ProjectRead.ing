// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace Api.Auth.Models;

[Table("user_tokens")]
public class UserTokenEF
{
    public const string PermissionBitsType = "PermissionBitsType";

    [ForeignKey(nameof(Owner))]
    public Guid UserId { get; set; }

    [Key]
    public required byte[] TokenHash { get; set; }

    public required UserPermissionBits Permissions { get; set; }

    public Instant CreationTime { get; set; }

    public Instant? LastUsed { get; set; }

    // Navigation Properties
    public UserEF Owner { get; set; } = null!;
}
