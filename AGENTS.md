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
dotnet test Api.Tests/Api.Tests.csproj --no-build -- \
  --treenode-filter "/*/*/FilesEndpointTests/*"   # REST endpoints: auth, ranges, multipart

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
├── Works/
│   ├── Models/            # EF entities: Work, WorkTag, Author, join entities
│   ├── Mutations/         # Add/Update mutations
│   ├── Queries/           # GraphQL query types, DTOs, mappers, data loaders
│   └── PGContext.cs       # DbSet declarations for Works domain
└── Files/
    ├── Endpoints/         # REST endpoints, one static handler per file
    ├── FileProviders/     # IFileProvider, LocalFSFileProvider, UserFileRouter, FileProviderFactory
    ├── Models/            # FileRecord, FileProviderBackendConfig
    ├── Mutations/         # Add metadata / add+upload / upload-content mutations
    ├── Queries/           # FileRecord query, node, data loaders, DTO, mapper
    ├── Setup.cs           # DI registration, endpoint mapping, backend seeding
    ├── PGContext.cs       # DbSet declarations for Files domain
    └── FileKind.cs        # FileKind enum

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
  `FilesTestBase` (extends `WorksTestBase`) adds `AuthenticatedClients` (returns both a
  GraphQL `ApiClient` and a raw `HttpClient` for the REST endpoints — set the
  `Authorization` header on **both**, a helper that authenticates only one silently
  401s the other), `FileRecordInput`, `UploadOf` (ZeroQL supports the `Upload` scalar:
  `new Upload(fileName, stream)`) and `AddFileRecordAsync`.
- **Convention**: every new query/mutation/connection should get a test in
  `WorksQueryTests`/`WorksMutationTests`; these are the coverage source for
  `Api/Works/Queries` and `Api/Works/Mutations`. For connections, include at least one
  test that requests `pageInfo { startCursor endCursor }` and pages with `after:` — a
  cursor-generation bug once survived multiple reviews because no test asked for `pageInfo`.
  When testing unknown-id errors, use `Guid.CreateVersion7().WithPostfix(0xFF)` (never a
  bare `CreateVersion7()`, whose random last byte can collide with a real postfix and
  make the test flaky).
  The same applies to `Api/Files` (`FilesQueryTests`/`FilesMutationTests`/
  `FilesEndpointTests`); `Api.Files.Setup.SeedDb` must run only where tables exist
  (dev path under `!IsTest`, test path after `CreateTablesAsync`) or every request fails
  with PostgreSQL `42P01` — it seeds the fallback LocalFS backend
  (`./BlobStorage{PR_TestPrefix}`, gitignored).

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

IdPostfixes enum: `User=0, Work=1, Author=2, WorkTag=3, FileRecord=4, FileProviderBackend=5`

### Permission Bits

```csharp
[Flags] enum UserPermissionBits : long {
    All = ~0,
    WorkRead = 1L << 0,     WorkWrite = 1L << 1,
    AuthorRead = 1L << 2,   AuthorWrite = 1L << 3,
    TagRead = 1L << 4,  TagWrite = 1L << 5,
    UserRead = 1L << 6,     UserWrite = 1L << 7,
    FileRead = 1L << 8,     FileWrite = 1L << 9,
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
        [Service] IEFTransactionDIAccessorService txGetter,
        CancellationToken ct,
        AddXxxInput input
    )
    {
        var tx = await txGetter.BeginOrGetTransactionAsync();
        var entity = AddXxxInputMapper.CreateFromDto(input, userId.Id!.Value, Now());
        await db.AddAsync(entity, cancellationToken: ct);
        await db.SaveChangesAsync(cancellationToken: ct);
        await tx.CommitAsync(ct);
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

    // Field typing rules for partial updates (IMPORTANT - see "Update input field typing"):
    public required Optional<string> FieldName { get; init; }      // required on entity -> T!, must be sent
    public Optional<string?> Description { get; init; }             // nullable column -> omittable, null clears
    public required Optional<List<Guid>?> TagIds { get; init; }    // collection -> omittable, null clears

        public class UpdateXxxInputValidator : AbstractValidator<UpdateXxxInput>
        {
            public UpdateXxxInputValidator(
                PGContext db, ICurrentUserId userId, IEFTransactionDIAccessorService tx
            )
            {
                RuleFor(w => w).BeginTransaction(tx);
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

#### Update input field typing

How `Optional<T>` maps to the GraphQL schema (verified against exported `Api/Schema.graphql`):

| C# declaration                                    | Schema field          | Client behavior                                    |
|---------------------------------------------------|-----------------------|----------------------------------------------------|
| `required Optional<T>` (T non-nullable)           | `T!`                  | must be sent on **every** update; explicit null rejected by schema |
| `Optional<T?>` / `Optional<List<Guid>?>`            | `T` / `[T]`           | omittable; explicit `null` means "clear"           |
| `required Optional<string[]>` etc. (collections)     | `[T!]!`               | must be sent; nulls rejected                        |

> **Never add `[DefaultValue(...)]` to make a required field omittable.** It turns the
> schema field nullable (`title: String = ""`): an explicit `title: null` then passes
> schema validation and crashes on the NOT NULL column (raw `Unexpected Execution Error`),
> and spec-compliant clients may materialize the default ("" here) and silently wipe the
> field. Omission only stays a "no-op" because HotChocolate leaves `Optional<T>` unspecified
> when the field is absent — the schema default is a lie to clients about what omitted means.
> We chose the consistent contract instead: required-on-entity fields are `T!` and must be
> re-sent on every update (see `UpdateWorkInput.Title`, `UpdateAuthorInput.DisplayName`).

ZeroQL's generated client omits `null` input fields, so partial updates through it are safe;
raw GraphQL clients can still send explicit nulls — that's why required fields must be `T!`.

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

### Files Slice (immutable blob storage)

`FileRecord` is an immutable metadata row for a stored blob; `Uploaded` distinguishes a
reserved record (metadata only) from one whose bytes are stored. There is deliberately **no
update mutation** — the only state transition (`Uploaded`, plus `RowVersion`/`MetadataUpdatedAt`)
happens inside `UserFileRouter.SetFileAsync`, not through the API.

- **Router contract** (`Api/Files/FileProviders/UserFileRouter.cs`, scoped):
  `CreateFileAsync` picks the backend via `GetUsersPrefferedStorage` and returns the tracked
  entity; `GetFileAsync` returns `FileContent?` (`Stream`, `ContentType`, `OriginalFileName`,
  `SHA256`, `MetadataAddedAt`) or `null` when the row is missing / not uploaded / the provider
  is unknown; `SetFileAsync` returns the updated `FileRecord`, or `null` when the provider
  rejected the content. Providers MUST verify both the declared size and the SHA-256
  (`LocalFSFileProvider.SetFileAsync` rejects `totalBytesRead != sizeBytes` and hash mismatch;
  `totalBytesRead` is a `long` — an `int` wraps past 2 GiB and silently defeats both checks).
- **Mutations** (three, sharing `AddFileRecordInput`, and no Mapperly input mapper — the
  router builds the entity, don't duplicate construction): `addFileRecordMutation` (metadata
  only), `addFileRecordWithFileMutation` (metadata + `Upload`; on failure it throws
  `FILE_UPLOAD_FAILED` *before* `CommitAsync`, so the transaction removes the row — no
  explicit delete, and LocalFS only `File.Move`s its temp file after the hash matches),
  `uploadFileRecordContentMutation` (content for a reserved record; `ID_DOES_NOT_EXIST` /
  `FILE_ALREADY_UPLOADED` via the input validator).
- **`sha256` on the wire is lowercase hex**, exposed by a value resolver on `FileRecordNode`
  (`Convert.ToHexStringLower`) over a `[GraphQLIgnore] byte[]` DTO member. Never put the
  conversion inside `ProjectToDto` — Npgsql has no translator for `Convert.ToHexString*`, so
  it fails at query time under `ToPageWithDataLoaderAsync`.
- **REST** (`Api/Files/Endpoints/`, one static handler per file, mapped from
  `Api/Files/Setup.MapEndpoints` under `/api/files`):
  - `GET /api/files/{id}` — `Results.File(..., enableRangeProcessing: true)` gives
    `Accept-Ranges`/`206`/`If-Range`; needs a seekable stream (LocalFS returns
    `File.OpenRead`). Headers: `Cache-Control: private, max-age=31536000, immutable`
    (blobs are content-addressed and never rewritten — a change is a new record/id) and
    `X-Content-Type-Options: nosniff` (the stored content type is client-declared). Inline
    only for the whitelist (`image/avif`, `image/jpeg`, `application/epub+zip`); everything
    else is `application/octet-stream` +
    `attachment; filename="<originalFileName ?? recordId>"`.
  - `POST /api/files/{id}` — multipart `IFormFile`; 404 / 409 / 422 / 204.
- **Auth on minimal-API handlers**: `[PermissionCheckAuthorize(...)]` works as an attribute
  on the handler method because `RouteEndpointDataSource` copies handler-method attributes
  into endpoint metadata — no `.RequireAuthorization` chaining. `[FromForm]`/`IFormFile`
  endpoints need `.DisableAntiforgery()` (the app registers no antiforgery services), and
  per-endpoint limits are attributes too (`[RequestFormLimits]` implements
  `IFormOptionsMetadata`, `[RequestSizeLimit]` implements `IRequestSizeLimitMetadata`).
- **Uploads**: the `Upload` scalar needs `.AddUploadType()` in `Program.cs`. Multipart limits
  are endpoint metadata and `/graphql` honors them too (`DefaultHttpRequest` builds the form
  feature with `HttpContext.GetEndpoint()`), so `MapGraphQL()` gets the same
  `RequestFormLimitsAttribute` via `.WithMetadata(...)`. Kestrel `MaxRequestBodySize` is 5 GiB
  — its 30 MiB default would 413 before any form limit applies.
`Api.Files.Setup.SeedDb` inserts the fallback LocalFS backend config on startup (see Testing).

### Validation Helpers (Api.Utils.ValidatorUtils)

- `BeginTransaction(tx)` - begin the shared transaction as the validator's first rule
- `MustBeDistinct(propName)` - collection items must be unique
- `IdsMustExist(dbSet, ownerId?)` - all GUIDs must exist in DbSet
- `IdMustExist(dbSet, ownerId?)` - single GUID must exist in DbSet
- `RowVersionMustMatch(dbSet)` - row version must match (requires `T : IBasicEntityMetadata`)
- `WhenOptionalSet(expr)` - apply rule only when `Optional<T>.HasValue` is true

### Validator Lifetimes & the Shared Transaction (FairyBread)

Validators are registered by `AddValidatorsFromAssemblyContaining<Program>()` (scoped) and
resolved by FairyBread from the **GraphQL request scope**. This is load-bearing:

- The validator and the mutation resolver therefore share the **same** `PGContext` and
  `IEFTransactionDIAccessorService`. The first validator rule calls
  `tx.BeginOrGetTransactionAsync()` so that validation reads (row version check, duplicate
  checks) and the mutation's `CommitAsync` are one unit.
- **Never implement `IRequiresOwnScopeValidator` on these validators.** FairyBread then
  resolves them in a fresh scope with a *different* `PGContext`, silently breaking the
  shared-transaction pattern (it also churns an extra `DbContext` per validation). It was
  removed from `AddWorkInputValidator` and `RegisterInputValidator` after a review.

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

// Api/Files/PGContext.cs
public DbSet<FileRecord> FileRecords { get; set; }
public DbSet<FileProviderBackendConfig> FileProviderBackendConfigs { get; set; }
```

### EF Entity Models

| Model    | Table           | Key Fields                                                        | IdPostfix |
|----------|-----------------|-------------------------------------------------------------------|-----------|
| Work     | works           | Title, Description?, WorkPublishedAt?, WorkUpdatedAt?, WorkIdentifiers | 1         |
| WorkTag  | work_tags       | TagNamespace (string[]), TagName                                  | 3         |
| Author   | authors         | FirstName?, LastName?, DisplayName, PenNames (List<string>)       | 2         |
| FileRecord                | file_records    | FileKind, FileProviderBackendConfigId, Uploaded, OriginalFileName?, ContentType, SizeBytes, SHA256 | 4         |
| FileProviderBackendConfig | file_provider_backend_configs | OwnerId?, InstanceWide, FallbackDefault, DefaultFor (FileKind[]), ProviderId, ProviderConfig (JsonDocument?) | 5 |

### Conventions

- All entities are tenant-scoped (filtered by `OwnerId == userId.Id`)
- `Now()` from `Api.Utils.GeneralUtils` returns `NodaTime.Instant`
- SPDX license headers on all files
- Query class names use `Query`
- All mutations use transactions via `IEFTransactionDIAccessorService`; validators call
  `RuleFor(...).BeginTransaction(tx)` as their first rule so validation reads and the
  mutation's `CommitAsync` are one unit (see "Validator Lifetimes & the Shared Transaction")
- Navigation properties on EF models should have `[MapperIgnore]` or be ignored in mapper with `[MapperIgnoreTarget]`/`[MapperIgnoreSource]`
- Tag uniqueness is guarded three ways: friendly `TAG_ALREADY_EXISTS` FluentValidation
  checks (Add/UpdateTag), the DB unique index on `(OwnerId, TagNamespace, TagName)`
  (`Api/Works/Models/WorkTag.cs`), and `db.SaveChangesOrThrowAsync(sqlState, code, message, ct)`
  (`Api/Utils/DbExceptionUtils.cs`) which maps a PG error with that `SqlState` at
  `SaveChangesAsync` to a `GraphQLException` with that code (`SetCode`, i.e. `extensions.code`)
  and message — the catch covers the validator's check-then-insert race under READ COMMITTED.
  Use this helper (not a naked `SaveChangesAsync`) when a mutation writes a row guarded by a
  unique index; pass a `null` `sqlState` to match any PostgreSQL error.
- GraphQL cost model: `Filtering`/`Sorting` `VariableMultiplier = 1` and `MaxFieldCost`/
  `MaxTypeCost = 20_000` are REQUIRED for nested connection filtering (`Tag.works(where:)`,
  `Author.works(where:)`) — the defaults plus a 5k cap rejected such queries with `HC0047`.
  Real depth guards are `MaxPageSize = 50` + `RequirePagingBoundaries = true`.
- HotChocolate/GreenDonut are pinned to prerelease `16.7.0-p.5` for the `[DataLoaderModule]`
  source-generated modules — track releases and bump to stable when available.
- A GraphQL field-name gotcha: HotChocolate strips only the `Async` suffix, so
  `UpdateWorkMutation` becomes `updateWorkMutation` (NOT `updateWork`).
- REST endpoints live in `Api/<Slice>/Endpoints/`, one static handler per file, with the
  auth policy and `[RequestFormLimits]` as attributes on the handler function (see the
  Files Slice section for why attribute-on-handler works)
- When a domain service already constructs the entity (e.g. `UserFileRouter.CreateFileAsync`),
  mutations call it instead of adding a Mapperly input mapper that duplicates construction
- AI-authored regions are wrapped in `// Mostly Ai Generated - Start` / `- End` markers
- Binary values (SHA-256) go over the wire as lowercase hex via a node value resolver — never
  as `byte[]` (serializes as `Base64String`) and never converted inside `ProjectToDto`
- The app is Linux-only: `[assembly: SupportedOSPlatform("linux")]` on both `Api` and
  `Api.Tests` (per-class annotations cascade CA1416 up the whole call chain — annotate the
  assembly instead)
