# SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
#
# SPDX-License-Identifier: AGPL-3.0-or-later
# SPDX-License-Identifier: Apache-2.0

{
  inputs = {
    flake-parts.url = "github:hercules-ci/flake-parts";
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    process-compose-flake.url = "github:Platonic-Systems/process-compose-flake";
    services-flake.url = "github:juspay/services-flake";
  };

  outputs =
    inputs@{ flake-parts, ... }:
    flake-parts.lib.mkFlake { inherit inputs; } {
      imports = [
        inputs.process-compose-flake.flakeModule
      ];
      systems = [
        "x86_64-linux"
        "aarch64-linux"
        "aarch64-darwin"
        "x86_64-darwin"
      ];
      perSystem =
        {
          config,
          self',
          inputs',
          pkgs,
          system,
          ...
        }:
        {
          devShells.default =
            let
              dotnet = (
                with pkgs.dotnetCorePackages;
                combinePackages [
                  sdk_10_0
                ]
              );
            in
            pkgs.mkShell {
              inputsFrom = [
                config.process-compose."dev-services".services.outputs.devShell
              ];
              packages = with pkgs; [
                dotnet
                nodejs_22
                reuse
                just
                wrangler
              ];

              DOTNET_ROOT = "${dotnet}/share/dotnet";
              DOTNET_PATH = "${dotnet}/bin/dotnet";
            };
          devShells.ci = config.devShells.default;

          process-compose =
            let
              common = {
                imports = [
                  inputs.services-flake.processComposeModules.default
                ];

                cli.options = {
                  no-server = false;
                };

                services.postgres."pg1" = {
                  package = pkgs.postgresql_18;
                  superuser = "postgres";
                  extensions = exts: [
                    exts.system_stats
                  ];
                  initialScript.before = ''
                    CREATE EXTENSION system_stats;
                  '';
                  enable = true;
                };

              };
            in
            {
              "ci-services" = {
                imports = [ common ];
              };
              "dev-services" = {
                imports = [ common ];
                services.pgadmin."pgad1" = {
                  enable = true;
                  initialEmail = "email@example.com";
                  initialPassword = "123";
                  extraConfig = {
                    SERVER_MODE = false;
                    CONFIG_DATABASE_URI = "postgresql://postgres@localhost";
                  };
                };
              };
            };
        };
    };
}
