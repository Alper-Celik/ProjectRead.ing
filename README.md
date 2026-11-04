<!--
SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>

SPDX-License-Identifier: AGPL-3.0-or-later OR Apache-2.0
-->
# ProjectRead.ing

[![code coverage](https://codecov.projectread.ing/badge_shieldsio_linecoverage_blue.svg)](<https://codecov.projectread.ing/>)

[Latest Test Run on `main`](https://codecov.projectread.ing/Api.Tests-linux-net11.0-report.html)

<!-- Mostly Ai Generated - Start -->
## Bazel build

`//Api:project_reading_api_server` builds the API with rules_dotnet + rules_nixpkgs:

```sh
bazel build //Api:project_reading_api_server   # from the devshell (needs coreutils in PATH)
bazel run //Api:project_reading_api_server     # ASPNETCORE_ENVIRONMENT=Development for the dev PG
```

What's wired:

- `flake.nix` exports `packages.bazel-dotnet-sdk`; rules_nixpkgs builds it into the
  `@dotnet_sdk` repo and `dotnet/nix_sdk_repository.bzl` wraps it into a rules_dotnet
  `dotnet_toolchain` (no rules_dotnet Microsoft-SDK download). nix must be on PATH.
- `deps/` holds the NuGet repos plus `deps/Api_deps.bzl` (the per-project dep
  label list `Api/BUILD.bazel` loads). Both are generated from
  `paket.dependencies`/`paket.lock` + `Api/paket.references` by
  `just bazel-sync-deps`; `just bazel-deps-check` (part of `just lint`) fails when
  they are stale.
- `dotnet/` contains small patches to rules_dotnet 0.22.2 applied via
  `archive_override` (Bazel 9 shell-env compat + nixos /usr/bin and a nuget
  analyzer-selection fix).
- `Api/BUILD.bazel` supplies global usings explicitly (`Api/GlobalUsings.cs`); the
  csproj has `ImplicitUsings=disable` so both build paths behave the same.
<!-- Mostly Ai Generated - End -->

WIP
