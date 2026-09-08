# SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
#
# SPDX-License-Identifier: AGPL-3.0-or-later
# SPDX-License-Identifier: Apache-2.0

color-cmd := "44m"

lint: dotnet-restore dotnet-format-check lint-reuse



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
  dotnet format style --verify-no-changes --verbosity diagnostic --no-restore
  dotnet format analyzers --verify-no-changes --verbosity diagnostic --no-restore

dotnet-format:
  dotnet tool restore
  dotnet csharpier .
  dotnet format style --verbosity diagnostic --no-restore
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
