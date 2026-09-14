<!--
SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>

SPDX-License-Identifier: AGPL-3.0-or-later
SPDX-License-Identifier: Apache-2.0
-->

<!-- Mostly Ai Generated -->
# ProjectRead.ing - Agent Reference

## Build & Test Commands

```bash
dotnet tool restore                      # REQUIRED first: restores zeroql.cli, csharpier, dotnet-ef, reportgenerator
dotnet build Api/Api.csproj              # also exports Api/Schema.graphql (AfterBuild target)
dotnet build Api.Tests/Api.Tests.csproj  # regenerates the ZeroQL client from Api/Schema.graphql

dotnet test Api.Tests/Api.Tests.csproj --no-build -- \
  --treenode-filter "/*/*/WorksQueryTests/*"   # TUnit/Microsoft Testing Platform filter

just dotnet-build   # dotnet restore --locked-mode + build
just dotnet-test    # runs tests with coverage (cobertura) + reportgenerator HTML/badges
just lint           # reuse lint + csharpier check + dotnet format analyzers
just format         # csharpier format + dotnet format analyzers

dotnet csharpier check <files>              # formatting gate used by CI
```

> `zeroql.cli` must be restored (`dotnet tool restore`) or the test project fails to
> build with "ZeroQL CLI could not be found".

## Project Structure

```
Api/
├── Auth/
│   ├── Handlers/          # PermissionCheckAuthorize attribute
│   ├── Models/            # UserEF, UserPermissionBits
│   └── Utils/             # ICurrentUserId
├── Database/
│   ├── PGContext.cs        # Main DbContext (partial)
│   └── Utils/             # IEFTransactionDIAccessorService
├── Graphql/               # ErrorCodes, GuidNodeSerializer
├── Utils/                 # ValidatorUtils, EntityMetadata, IdPostfixes, MapperUtils, GeneralUtils
└── Works/
    ├── Models/            # EF entities: Work, WorkTag, Author, join entities
    ├── Mutations/         # Add/Update mutations
    ├── Queries/           # GraphQL query types, DTOs, mappers, data loaders
    └── PGContext.cs       # DbSet declarations for Works domain
```

## Testing

`Api.Tests/` covers the GraphQL surface end-to-end through a strongly-typed ZeroQL client
against a real PostgreSQL instance.

- **Framework**: TUnit on the Microsoft Testing Platform (`[Test]` methods, filter with
  `--treenode-filter "/*/*/ClassName/*"`), plus `TUnit.AspNetCore`'s
  `WebApplicationTest` + `WebApplicationFactory<Program>`.
- **Client**: `ZeroQL` generates `ApiClient` from `Api/Schema.graphql` using
  `Api.Tests/config.zeroql.json`. `dotnet build Api/Api.csproj` exports the schema
  (AfterBuild target); if you add/change a GraphQL field, rebuild **Api** before the test
  project or the generated client is stale and throws "Schema can be outdated".
- **Database**: tests need PostgreSQL (18) reachable at `localhost:5432`. Bring it up with
  `nix run .#dev-services -- up` (or `.#ci-services`). `PG_PASSWORD` comes from `.env`;
  the `PG` connection string is read from appsettings and overridden per test.
  `TestInit`/`DBData` create and drop an isolated `test_db` + schema per test session, and
  delete the database on dispose — never point tests at a shared database.
- **Helpers**: `WorksTestBase` (extends `TestInit`) provides `AuthenticatedClient`,
  `AddAuthor`/`AddTag`/`AddWork`, `Register`/`Login`, `NodeIdToGuid`, and `AssertErrorCode`.
- **Convention**: every new query/mutation/connection should get a test in
  `WorksQueryTests`/`WorksMutationTests`; these are the coverage source for
  `Api/Works/Queries` and `Api/Works/Mutations`.

## Key Patterns

### Entity Metadata Hierarchy

```
IBasicEntityMetadata  →  static abstract byte IdPostfix, Guid Id, int RowVersion
IEntityMetadata       →  + NodaTime.Instant MetadataAddedAt, MetadataUpdatedAt
IDbEntityMetadata     →  + Guid OwnerId
```

### ID Generation

```csharp
var id = Guid.CreateVersion7().WithPostfix(Models.Work.IdPostfix);
```

IdPostfixes enum: `User=0, Work=1, Author=2, WorkTag=3`

### Permission Bits

```csharp
[Flags] enum UserPermissionBits : long {
    All = ~0,
    WorkRead = 1L << 0,     WorkWrite = 1L << 1,
    AuthorRead = 1L << 2,   AuthorWrite = 1L << 3,
    TagRead = 1L << 4,  TagWrite = 1L << 5,
    UserRead = 1L << 6,     UserWrite = 1L << 7,
}
```

### Add Mutation Pattern

```csharp
[MutationType]
public static partial class AddXxxMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.XxxWrite)]
    public static async Task<AddXxxPayload> AddXxxMutation(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        CancellationToken ct,
        AddXxxInput input
    )
    {
        var entity = AddXxxInputMapper.CreateFromDto(input, userId.Id!.Value, Now());
        await db.AddAsync(entity, cancellationToken: ct);
        await db.SaveChangesAsync(cancellationToken: ct);
        return new AddXxxPayload(XxxMapper.ToDto(entity));
    }
}

public record AddXxxPayload(Queries.Xxx Xxx);

public record AddXxxInput
{
    // plain (non-Optional) required fields
}

[Mapper]
public static partial class AddXxxInputMapper
{
    [MapperIgnoreTarget(nameof(Models.Xxx.RowVersion))]
    [MapperIgnoreTarget(nameof(Models.Xxx.NavigationProp1))]
    // ... ignore all navigation properties
    private static partial Models.Xxx CreateFromDtoInternal(
        AddXxxInput w, Guid id, Instant metadataAddedAt, Instant metadataUpdatedAt
    );

    public static Models.Xxx CreateFromDto(AddXxxInput w, Guid ownerId, Instant now)
    {
        var id = Guid.CreateVersion7().WithPostfix(Models.Xxx.IdPostfix);
        var entity = CreateFromDtoInternal(w, id, now, now);
        entity.OwnerId = ownerId;
        return entity;
    }

    [UserMapping(Default = true)]
    public static ZonedDateTime FromInstantToZonedDateTime(Instant i) =>
        MapperUtils.FromInstantToZonedDateTime(i);
}
```

### Update Mutation Pattern

```csharp
[MutationType]
public static partial class UpdateXxxMutations
{
    [PermissionCheckAuthorize(UserPermissionBits.XxxRead | UserPermissionBits.XxxWrite)]
    public static async Task<UpdateXxxPayload> UpdateXxxMutation(
        [Service] PGContext db,
        [Service] IEFTransactionDIAccessorService txGetter,
        UpdateXxxInput input,
        CancellationToken ct
    )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync();
        var entity = await db.Xxxs.SingleAsync(e => e.Id == input.Id, cancellationToken: ct);

        UpdateXxxInput.ApplyToXxx(entity, input);
        entity.RowVersion += 1;
        entity.MetadataUpdatedAt = Now();

        await db.SaveChangesAsync(cancellationToken: ct);
        await tx.CommitAsync(ct);
        return new UpdateXxxPayload(XxxMapper.ToDto(entity));
    }
}

public record UpdateXxxPayload(Queries.Xxx Xxx);

public record UpdateXxxInput : IBasicEntityMetadata
{
    public static byte IdPostfix => Models.Xxx.IdPostfix;
    public Guid Id { get; init; }
    public int RowVersion { get; init; }

    // All mutable fields use Optional<T> for partial updates
    public required Optional<string> FieldName { get; init; }

    public class UpdateXxxInputValidator : AbstractValidator<UpdateXxxInput>
    {
        public UpdateXxxInputValidator(
            PGContext db, ICurrentUserId userId, IEFTransactionDIAccessorService tx
        )
        {
            RuleFor(w => w).MustAsync(async (_, ct) => { await tx.BeginOrGetTransactionAsync(); return true; });
            RuleFor(w => w.Id).IdMustExist(db.Xxxs, userId.Id);
            RuleFor(w => w.RowVersion).RowVersionMustMatch(db.Xxxs);
        }
    }

    public static void ApplyToXxx(Models.Xxx entity, UpdateXxxInput input)
    {
        if (input.FieldName.HasValue)
            entity.FieldName = input.FieldName.Value;
    }
}
```

### Query/DTO Pattern

```csharp
[QueryType]
public static partial class XxxQuery
{
    [PermissionCheckAuthorize(UserPermissionBits.XxxRead)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Xxx>> GetXxxs(
        [Service] PGContext db,
        [Service] ICurrentUserId userId,
        [Service] XxxNode.IXxxByIdDataLoader xxxById,
        QueryContext<Xxx> qc,
        PagingArguments pg,
        CancellationToken ct
    ) => await db.Xxxs.Where(x => x.OwnerId == userId.Id)
        .ProjectToDto().WithQueryContext(qc).ToPageWithDataLoaderAsync(pg, xxxById, ct);
}

[ObjectType<Xxx>]
public static partial class XxxNode
{
    public interface IXxxByIdDataLoader : IBatchDataLoader<Guid, Xxx>;

    [DataLoader<IXxxByIdDataLoader>]
    public static async Task<IDictionary<Guid, Xxx>> GetXxxByIdAsync(
        IReadOnlyList<Guid> ids, [Service] PGContext db,
        [Service] ICurrentUserId userId, CancellationToken ct
    ) => await db.Xxxs.Where(x => x.OwnerId == userId.Id)
        .Where(x => ids.Contains(x.Id)).ProjectToDto()
        .ToDictionaryAsync(x => x.Id, ct);

    [PermissionCheckAuthorize(UserPermissionBits.XxxRead)]
    [GraphQLIgnore]
    public static async Task<Xxx?> GetByIdAsync(
        IXxxByIdDataLoader xxxById, Guid id, CancellationToken ct
    ) => await xxxById.LoadAsync(id, ct);
}

[Node(NodeResolverType = typeof(XxxNode), NodeResolver = nameof(XxxNode.GetByIdAsync))]
public class Xxx : IEntityMetadata, INode
{
    public static byte IdPostfix => Models.Xxx.IdPostfix;
    public Guid Id { get; set; }
    public int RowVersion { get; set; }
    public NodaTime.Instant MetadataAddedAt { get; set; }
    public NodaTime.Instant MetadataUpdatedAt { get; set; }
    // ... domain fields
}

[Mapper]
public static partial class XxxMapper
{
    [MapperIgnoreSource(nameof(Models.Xxx.NavigationProp1))]
    // ... ignore navigation properties
    public static partial Xxx ToDto(Models.Xxx x);
    public static partial IQueryable<Xxx> ProjectToDto(this IQueryable<Models.Xxx> q);
}
```

### Nested Connections & M2M Data Loaders

Nested fields live on the `[ObjectType<T>]` node class, take `[Parent]`, and resolve ids
through a `IBatchDataLoader<Guid, Guid[]>` so N+1 is avoided. Filtering/sorting/pagination
on a nested connection go through `ToPageWithDataLoaderAsync(pg, itemById, ct)`.

```csharp
[PermissionCheckAuthorize(UserPermissionBits.TagRead | UserPermissionBits.WorkRead)]
[GraphQLName("Tags")]
public static async Task<IReadOnlyList<Tag>> GetTagsByWorkAsync(
    [Parent] Work work,
    ITagIdsByWorkIdDataLoader tagIdsByWorkLoader,
    TagNode.ITagByIdDataLoader tagByIdLoader,
    int? limit,
    CancellationToken ct
)
{
    var ids = await tagIdsByWorkLoader.LoadAsync(work.Id, ct);
    if (limit is not null)
        ids = [.. ids?.Take((int)limit) ?? []];
    ids ??= []; // loader has no entry for a parent with no rows
    return (await tagByIdLoader.LoadAsync(ids, ct))!;
}

// Connection variant (TagNode):
[PermissionCheckAuthorize(UserPermissionBits.WorkRead)]
[UseFiltering]
[UseSorting]
[GraphQLName("Works")]
public static async Task<PageConnection<Work>> GetWorksByTag(
    [Parent] Tag tag, [Service] PGContext db, [Service] ICurrentUserId userId,
    [Service] IWorkByIdDataLoader workById, QueryContext<Work> qc,
    PagingArguments pagingArguments, CancellationToken ct
) => await db.Works.Where(w => w.OwnerId == userId.Id)
    .Where(w => w.WorkTags.Select(t => t.Id).Contains(tag.Id))
    .ProjectToDto().WithQueryContext(qc)
    .ToPageWithDataLoaderAsync(pagingArguments, workById, ct);
```

The id data loader delegates to `GeneralUtils.GetManyToManyIds` (`Api/Utils/GeneralUtils.cs`),
which applies `OwnerId` tenant scoping and expands the M2M selector expression (LinqKit
`WithExpressionExpanding` is enabled on the context in `Api/Database/PGContext.cs`):

```csharp
await db.Works.GetManyToManyIds(
    ids,
    (Guid)userId.Id!,
    w => w.WorkTag_Works.Select(wt => wt.WorkTagId),
    ct
);
```

> Always null-coalesce loader results before passing to the item loader — `GetManyToManyIds`
> simply omits parents that have no join rows.

### Validation Helpers (Api.Utils.ValidatorUtils)

- `MustBeDistinct(propName)` - collection items must be unique
- `IdsMustExist(dbSet, ownerId?)` - all GUIDs must exist in DbSet
- `IdMustExist(dbSet, ownerId?)` - single GUID must exist in DbSet
- `RowVersionMustMatch(dbSet)` - row version must match (requires `T : IBasicEntityMetadata`)
- `WhenOptionalSet(expr)` - apply rule only when `Optional<T>.HasValue` is true

### Key Libraries

- **HotChocolate** - GraphQL server (provides `Optional<T>`, `[Node]`, `[QueryType]`, `[MutationType]`, etc.)
- **Riok.Mapperly** - compile-time object mapper (`[Mapper]`, `[MapperIgnoreTarget]`, `[MapperIgnoreSource]`)
- **FluentValidation** - input validation (`AbstractValidator<T>`)
- **FairyBread** - automatic input validation for HotChocolate
- **NodaTime** - date/time handling (`Instant`, `ZonedDateTime`)
- **Entity Framework Core** - ORM with PostgreSQL
- **GreenDonut** - data loader library for HotChocolate

### EF DbSets (PGContext partial)

```csharp
// Api/Works/PGContext.cs
public DbSet<Work> Works { get; set; }
public DbSet<WorkTag> WorkTags { get; set; }
public DbSet<Author> Authors { get; set; }

// Api/Auth/PGContext.cs
public DbSet<UserEF> Users { get; set; }
public DbSet<UserTokenEF> UserTokens { get; set; }
```

### EF Entity Models

| Model    | Table           | Key Fields                                                        | IdPostfix |
|----------|-----------------|-------------------------------------------------------------------|-----------|
| Work     | works           | Title, Description?, WorkPublishedAt?, WorkUpdatedAt?, WorkIdentifiers | 1         |
| WorkTag  | work_tags       | TagNamespace (string[]), TagName                                  | 3         |
| Author   | authors         | FirstName?, LastName?, DisplayName, PenNames (List<string>)       | 2         |

### Conventions

- All entities are tenant-scoped (filtered by `OwnerId == userId.Id`)
- `Now()` from `Api.Utils.GeneralUtils` returns `NodaTime.Instant`
- SPDX license headers on all files
- Query class names use `Query`
- Update mutations use transactions via `IEFTransactionDIAccessorService`
- Add mutations do NOT use transactions
- Navigation properties on EF models should have `[MapperIgnore]` or be ignored in mapper with `[MapperIgnoreTarget]`/`[MapperIgnoreSource]`
