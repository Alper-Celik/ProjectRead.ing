// SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Api.Auth.Models;
using Api.QueryTypes;
using Api.Utils;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riok.Mapperly.Abstractions;

namespace Api.Works.Models;

[Table("works")]
public class Work
    : IDbEntityMetadata,
        IHasConnection<Work, Author>,
        IHasConnection<Work, WorkTag>
{
    public static byte IdPostfix => (byte)IdPostfixes.Work;

    [MapperIgnore]
    public static IReadOnlyDictionary<Type, LambdaExpression> Connections =>
        new Dictionary<Type, LambdaExpression>([
            new(typeof(Author), static (Work w) => w.Authors),
            new(typeof(WorkTag), static (Work t) => t.WorkTags),
        ]);

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

public record WorkFilter : LogicalFilter<Work, WorkFilterPart, WorkFilter>;

//TODO: add text searches with VectorChord-bm25 when fts and embedding work starts
public record WorkFilterPart : EntityMetadataFilter<Work>
{
    public TimeFilter? WorkPublishedAt
    {
        get { return (TimeFilter?)GetOneOf(); }
        set { SetOneOf(value, w => w.WorkPublishedAt, nameof(Work.WorkPublishedAt)); }
    }

    public TimeFilter? WorkUpdatedAt
    {
        get { return (TimeFilter?)GetOneOf(); }
        set { SetOneOf(value, w => w.WorkUpdatedAt, nameof(Work.WorkUpdatedAt)); }
    }

    public BasicStringFilter? Title
    {
        get { return (BasicStringFilter?)GetOneOf(); }
        set { SetOneOf<string?>(value, w => w.Title); }
    }

    public BasicStringFilter? Description
    {
        get { return (BasicStringFilter?)GetOneOf(); }
        set { SetOneOf<string?>(value, w => w.Description); }
    }
}
