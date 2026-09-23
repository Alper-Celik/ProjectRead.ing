// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Auth.Models;

namespace Api.RemoteServices.Models;

[Table("remote_services")]
public class RemoteService : IEntityMetadata
{
    public static byte IdPostfix => (byte)IdPostfixes.RemoteService;

    [Key]
    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    public required string Name { get; set; }

    public bool IsInstanceService { get; set; }

    [ForeignKey(nameof(Owner))]
    public Guid? OwnerId { get; set; }

    // Navigation Properties
    public UserEF? Owner { get; set; }
    public List<RemoteServiceUserPermission> UserPermissions { get; set; } = null!;
}
