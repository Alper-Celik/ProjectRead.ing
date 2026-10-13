<!-- Mostly Ai Generated -->
<!-- SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev> -->
<!--
SPDX-License-Identifier: AGPL-3.0-or-later OR Apache-2.0
-->

# TODO

## Tags / wrangling

- Consider converting `WorkTag` from `(TagNamespace: string[], TagName: string)` to a single
  `ltree` path (`Path ltree`, e.g. `ao3.romance.enemies_to_lovers`) — drop `TagName`, the tag
  value becomes the **last** path component, and every path prefix is itself a tag
  (structural parent/child instead of the accidental
  `(ns=[ao3], name=romance)` / `(ns=[ao3,romance], name=enemies2lovers)` coincidence).
  - Unique key moves to `(OwnerId, Path)`; keep the `SaveChangesOrThrowAsync`
    unique-violation handling, just point it at the new index.
  - Queries: descendants `path <@ 'ao3.romance'`, "tag anywhere" `path ~ '*.tag_name'`
    (both GiST-indexable).
  - Wrangling (AO3-style, see `otwcode/otwarchive`): canonical flag + `merger_id`-style
    self-FK (or `canonical_path ltree`) on the tag row; author-typed paths stay immutable,
    resolution follows the redirect; recomputed things (counts) are materialization, never
    query-time resolution. Skip AO3's `filter_taggings` denormalization — personal scale.
  - Machine tags live under an `auto` namespace segment so they don't collide with user
    tags; a tag-wrangling/veto system is planned for these.
  - Every namespace segment is user-visible (becomes a browsable tag) — pick segment names
    accordingly; use a visibility flag instead of invisible scaffolding.
  - Rename/merge of a parent rewrites the whole subtree — run as a background job (Jobs).
  - EF/Npgsql: enable `CREATE EXTENSION ltree` (migration + `HasPostgresExtension`); `LTree`
    struct ships in the main Npgsql EF provider (`Microsoft.EntityFrameworkCore` namespace).
    Docs: postgresql.org/docs/current/ltree.html (F.22),
    npgsql.org/efcore/mapping/translations.html.
- Related: vector search / auto-shelves — `SearchFilter` on `WorkFilterPart` (presence pins
  relevance ordering); LLM extraction pass at tagging stage emitting `{tags[], summary,
  embedding(summary)}` with raw extraction JSON kept for re-derivation.
