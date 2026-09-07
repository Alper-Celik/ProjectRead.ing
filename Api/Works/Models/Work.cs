// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Api.Auth.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Riok.Mapperly.Abstractions;

namespace Api.Works.Models;

[Table("works")]
public class Work : IDbEntityMetadata
{
    public static byte IdPostfix => (byte)IdPostfixes.Work;

    [Key]
    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }
    [MapperIgnore]
    [ForeignKey(nameof(Owner))]
    public Guid OwnerId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public NodaTime.ZonedDateTime? WorkPublishedAt { get; set; }
    public NodaTime.ZonedDateTime? WorkUpdatedAt { get; set; }
    public List<WorkIdentifier> WorkIdentifiers { get; set; } = [];

    // Navigation Properties
    [MapperIgnore]
    public List<WorkTag> WorkTags { get; set; } = [];
    [MapperIgnore]
    public List<Author> Authors { get; set; } = [];

    [MapperIgnore]
    public List<WorkTag_Work> WorkTag_Works { get; set; } = [];
    [MapperIgnore]
    public List<Work_Author> Work_Authors { get; set; } = [];

    [MapperIgnore]
    public UserEF Owner { get; set; } = null!;
}

public record WorkIdentifier(
        string WorkIdentifierType,
        string WorkIdentifierValue);

public class WorkTypeConfiguration : IEntityTypeConfiguration<Work>
{
    public void Configure(EntityTypeBuilder<Work> builder)
    {
        builder
            .ComplexCollection(w => w.WorkIdentifiers, wid => wid.ToJson())
            .HasIndex(w => w.WorkIdentifiers.Select(wid => wid));
    }
}