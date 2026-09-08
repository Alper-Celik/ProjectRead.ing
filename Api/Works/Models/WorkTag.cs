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

[Table("work_tags")]
[Index(nameof(OwnerId), nameof(TagNamespace), nameof(TagName), IsUnique = true)]
public class WorkTag : IDbEntityMetadata
{
    public static byte IdPostfix => (byte)IdPostfixes.WorkTag;

    [Key]
    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    [MapperIgnore]
    [ForeignKey(nameof(Owner))]
    public Guid OwnerId { get; set; }

    public required string[] TagNamespace { get; set; }

    public required string TagName { get; set; }

    // Navigation Properties
    public List<WorkTag_Work> WorkTagWorks { get; set; } = null!;
    public List<Work> Works { get; set; } = null!;
    public UserEF Owner { get; set; } = null!;
}

[Table("work_tag___tag")]
[PrimaryKey(nameof(WorkId), nameof(WorkTagId))]
public class WorkTag_Work
{
    public WorkTag_Work(Guid workId, Guid tagId)
    {
        WorkId = workId;
        WorkTagId = tagId;
    }

    public WorkTag_Work() { }

    public Guid WorkId { get; set; }
    public Guid WorkTagId { get; set; }

    // Navigation Properties
    public Work Work { get; set; } = null!;
    public WorkTag WorkTag { get; set; } = null!;
}

public class WorkTagTypeConfiguration : IEntityTypeConfiguration<WorkTag>
{
    void IEntityTypeConfiguration<WorkTag>.Configure(EntityTypeBuilder<WorkTag> builder)
    {
        builder
            .HasMany(wt => wt.Works)
            .WithMany(w => w.WorkTags)
            .UsingEntity(typeof(WorkTag_Work));
    }
}
