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

[Table("authors")]
public class Author : IDbEntityMetadata
{
    public static byte IdPostfix => (byte)IdPostfixes.Author;

    [Key]
    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }

    [MapperIgnore]
    [ForeignKey(nameof(Owner))]
    public Guid OwnerId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public required string DisplayName { get; set; }

    public required List<string> PenNames { get; set; }

    // Navigation Properties
    [MapperIgnore]
    public List<Work> Works { get; set; } = [];

    [MapperIgnore]
    public UserEF Owner { get; set; } = null!;
}

[Table("work___author")]
[PrimaryKey(nameof(WorkId), nameof(AuthorId))]
public class Work_Author
{
    public Work_Author(Guid workId, Guid authorId)
    {
        WorkId = workId;
        AuthorId = authorId;
    }

    public Work_Author() { }

    public Guid WorkId { get; set; }
    public Guid AuthorId { get; set; }

    // Navigation Properties
    public Work Work { get; set; } = null!;
    public Author Author { get; set; } = null!;
}

public class AuthorTypeConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder
            .HasMany(a => a.Works)
            .WithMany(w => w.Authors)
            .UsingEntity(typeof(Work_Author));
    }
}
