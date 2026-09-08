// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Auth.Models;

[Table("users")]
[Index(nameof(Email), IsUnique = true)]
public class UserEF : IEntityMetadata
{
    public static byte IdPostfix => (byte)IdPostfixes.User;

    [Key]
    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    public bool EmailVerified { get; set; } = false;

    public required string PasswordHash { get; set; }

    public bool Admin { get; set; } = false;
}

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<UserEF>
{
    public void Configure(EntityTypeBuilder<UserEF> builder)
    {
    }
}