# SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
#
# SPDX-License-Identifier: AGPL-3.0-or-later OR Apache-2.0

color-cmd := "44m"

bazel-deps-out := "deps"

lint: dotnet-restore dotnet-format-check lint-reuse bazel-deps-check

format: dotnet-format


lint-reuse:
  reuse lint

up:
  nix run .#dev-services -- up

dotnet-restore:
  dotnet restore --locked-mode
  dotnet tool restore

dotnet-build: dotnet-restore
  dotnet build --no-restore

dotnet-format-check:
  dotnet tool restore
  dotnet csharpier check .
  # dotnet format style --verify-no-changes --verbosity diagnostic --no-restore
  dotnet format analyzers --verify-no-changes --verbosity diagnostic --no-restore

dotnet-format:
  dotnet tool restore
  dotnet csharpier format .
  # dotnet format style --verbosity diagnostic --no-restore
  dotnet format analyzers --verbosity diagnostic --no-restore

dotnet-test:
  rm -rf TestResults
  dotnet test --no-restore -- --coverage --coverage-output-format cobertura --github-reporter-style full
  dotnet reportgenerator \
    "-reports:TestResults/*.cobertura.xml" \
    -targetdir:TestResults \
    "-reporttypes:Html;Badges" \
    -classfilters:+Api.\*

lint-dotnet: dotnet-restore dotnet-format-check dotnet-build

ci-all: lint-reuse lint-dotnet dotnet-test

ci-test: dotnet-test

# Regenerate deps/ from paket.dependencies/paket.lock (needs the devshell:
# nix + coreutils + bazel on PATH).
bazel-sync-deps:
  # Mostly Ai Generated - Start
  ./dotnet/bazel_sync_deps.sh "{{bazel-deps-out}}"
  # Mostly Ai Generated - End

# Fails with a diff when deps/ is stale relative to the paket files.
# (Runs from a temp clone of the generation logic so the real deps/ is never
# touched by the check itself.)
bazel-deps-check:
  #!/usr/bin/env bash
  # Mostly Ai Generated - Start
  set -euo pipefail
  tmp=$(mktemp -d)
  trap 'rm -rf "$tmp"' EXIT
  ./dotnet/bazel_sync_deps.sh "$tmp/deps" >/dev/null
  # deps/BUILD.bazel and the .license sidecars are hand-maintained, not generated.
  diff -r -x BUILD.bazel -x '*.license' "$(pwd)/{{bazel-deps-out}}" "$tmp/deps" \
    || { echo "bazel deps/ stale: run 'just bazel-sync-deps'"; exit 1; }
  # Mostly Ai Generated - End