// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace Api.Auth.Models;

[Table("remote_service_tokens")]
public class RemoteServiceToken : ITokenTime
{
    public const string ServiceIdentifierType = "ServiceIdentifierType";

    [Key]
    public required byte[] TokenHash { get; set; }

    [ForeignKey(nameof(RemoteService))]
    public Guid RemoteServiceId { get; set; }

    public Instant CreationTime { get; set; }

    public Instant? LastUsed { get; set; }

    // Navigation Properties
    public RemoteService RemoteService { get; set; } = null!;
}
