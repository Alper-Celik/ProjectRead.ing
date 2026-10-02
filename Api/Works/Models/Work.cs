// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Auth.Models;
using Api.QueryTypes;
using Api.Utils;
using LinqKit;
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

    public NodaTime.Instant? WorkPublishedAt { get; set; }
    public NodaTime.Instant? WorkUpdatedAt { get; set; }

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

//TODO: add text searches with VectorChord-bm25 when fts and embedding work starts
public record WorkFilter : EntityMetadataFilter<Work>
{
    public TimeFilter? WorkPublishedAt
    {
        get { return (TimeFilter?)GetOneOf(); }
        set { SetOneOf(value, w => w.WorkPublishedAt, nameof(Work.WorkPublishedAt)); }
    }

    public TimeFilter? WorkUpdatedAt
    {
        get { return (TimeFilter?)GetOneOf(); }
        set { SetOneOf(value, w => w.WorkPublishedAt, nameof(Work.WorkUpdatedAt)); }
    }
}
