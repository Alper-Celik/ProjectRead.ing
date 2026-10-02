// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Auth.Models;
using Api.Utils;
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

    // Mostly Ai Generated - Start
    // Tag components are joined/displayed with the `::` namespace separator, so
    // each component must be non-empty, contain no `::`, and have no leading or
    // trailing `:` — i.e. non-empty segments of non-`:` chars joined by single `:`.
    public const string ComponentPattern = @"^[^:]+(?::[^:]+)*$";

    // Mostly Ai Generated - End

    // Mostly Ai Generated - Start
    // Tag components are joined/displayed with the `::` namespace separator, so
    // each component must be non-empty, contain no `::`, and have no leading or
    // trailing `:` — i.e. non-empty segments of non-`:` chars joined by single `:`.
    // Enforced by the validator (FluentValidation), this attribute (via
    // EFCore.CheckConstraints) and the array CHECK below.
    [RegularExpression(ComponentPattern)]
    public required string TagName { get; set; }

    // Mostly Ai Generated - End

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
        // Mostly Ai Generated - Start
        // CHECK constraints can't contain subqueries, so element-wise validation of
        // the TagNamespace array is expressed by joining with an ASCII unit
        // separator and regex-matching the result: each component must be
        // non-empty, contain no `::`, and have no leading/trailing `:`. An empty
        // array (no namespace) is allowed.
        var us = "\u001F";
        var joinedPattern =
            $"^[^:{us}]+(?::[^:{us}]+|{us}[^:{us}]+)*$";
        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_work_tags_tag_namespace",
                $"cardinality(tag_namespace) = 0 OR array_to_string(tag_namespace, chr(31)) ~ '{joinedPattern}'"
            )
        );
        // Mostly Ai Generated - End
        builder
            .HasMany(wt => wt.Works)
            .WithMany(w => w.WorkTags)
            .UsingEntity(typeof(WorkTag_Work));
    }
}
