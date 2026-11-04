# SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
#
# SPDX-License-Identifier: AGPL-3.0-or-later OR Apache-2.0


# Mostly Ai Generated - Start
"""Repository rule that wraps a Nix-provided .NET SDK (flake.nix,
`packages.bazel-dotnet-sdk`, exposed through rules_nixpkgs) into the BUILD
layout rules_dotnet's `dotnet_toolchain` expects.

Replaces rules_dotnet toolchain repo (dotnet/repositories.bzl), which only
ships pinned Microsoft tarballs. Works from the layout produced by
`pkgs.dotnetCorePackages.combinePackages [ sdk_10_0 ]`:
  <nix-package-root>/bin/dotnet
  <nix-package-root>/share/dotnet/{sdk,shared,host,...}

F# is out of scope: this repo is C#-only, so `fsc` is wired via
`fsharp_compiler_path` only and never invoked by csharp_* rules.
"""

_ATTRIBUTES = {
    "sdk_repo": attr.string(
        doc = "rules_nixpkgs flake-package repo name exposing the Nix dotnet package (e.g. `dotnet_sdk`).",
        mandatory = True,
    ),
    "linux_x86_64_constraints": attr.label_list(
        default = [Label("@platforms//os:linux"), Label("@platforms//cpu:x86_64")],
    ),
    "linux_aarch64_constraints": attr.label_list(
        default = [Label("@platforms//os:linux"), Label("@platforms//cpu:aarch64")],
    ),
    "macos_x86_64_constraints": attr.label_list(
        default = [Label("@platforms//os:macos"), Label("@platforms//cpu:x86_64")],
    ),
    "macos_aarch64_constraints": attr.label_list(
        default = [Label("@platforms//os:macos"), Label("@platforms//cpu:aarch64")],
    ),
}

def _list_dir(repository_ctx, path):
    result = repository_ctx.execute(["ls", "-1", path])
    if result.return_code != 0:
        fail("Cannot list {path}: {msg}".format(path = path, msg = result.stderr))
    return [x for x in result.stdout.splitlines() if x]

def _single_dir(repository_ctx, path, what):
    entries = _list_dir(repository_ctx, path)
    if not entries:
        fail("No {what} found under {path}".format(path = path, what = what))
    if len(entries) > 1:
        fail("Ambiguous {what} (expected exactly one): {entries}".format(what = what, entries = entries))
    return entries[0]

def _toolchain_targets(platforms):
    targets = []
    for [platform, compatible_with] in platforms:
        targets.append("""
toolchain(
    name = "{platform}",
    exec_compatible_with = [{compatible_with}],
    target_compatible_with = [{compatible_with}],
    toolchain = ":dotnet_toolchain",
    toolchain_type = "@rules_dotnet//dotnet:toolchain_type",
)
""".format(
            platform = platform,
            compatible_with = ", ".join([
                '"%s"' % str(constraint)
                for constraint in compatible_with
            ]),
        ))
    return "".join(targets)

def _nix_dotnet_sdk_repository_impl(repository_ctx):
    repo_root = repository_ctx.path(Label("@{repo}//:BUILD.bazel".format(repo = repository_ctx.attr.sdk_repo))).dirname
    repository_ctx.symlink(repo_root.get_child("bin").get_child("dotnet"), "dotnet")
    repository_ctx.symlink(repo_root.get_child("share").get_child("dotnet"), "share/dotnet")

    sdk_version = _single_dir(repository_ctx, repository_ctx.path("share/dotnet/sdk"), "SDK")
    runtime_version = _single_dir(
        repository_ctx,
        repository_ctx.path("share/dotnet/shared/Microsoft.NETCore.App"),
        "runtime",
    )
    aspnet_version = _single_dir(
        repository_ctx,
        repository_ctx.path("share/dotnet/shared/Microsoft.AspNetCore.App"),
        "ASP.NET runtime",
    )
    runtime_tfm = "net" + ".".join(runtime_version.split(".")[:2])

    # Defaults from rules_dotnet's version matrix for SDK 10.x
    csharp_default_version = "14.0"
    fsharp_default_version = "10.0"

    build_content = """
load("@rules_dotnet//dotnet:toolchain.bzl", "dotnet_toolchain")
load("@rules_dotnet//dotnet:defs.bzl", "import_dll")

package(default_visibility = ["//visibility:public"])

filegroup(
    name = "csc_binary",
    srcs = ["share/dotnet/sdk/{sdk_version}/Roslyn/bincore/csc.dll"],
    data = glob([
        "share/dotnet/sdk/{sdk_version}/Roslyn/bincore/**/*",
        "share/dotnet/host/**/*",
    ]),
    visibility = ["//visibility:public"],
)

# Only needed by rules_dotnet internals that build F# code (e.g. the SDK
# generator assembly); our own builds target C# only.
filegroup(
    name = "fsc_binary",
    srcs = glob(
        [
            "share/dotnet/sdk/{sdk_version}/FSharp/fsc.dll*",
            "share/dotnet/sdk/{sdk_version}/FSharp/fsc.exe*",
        ],
        allow_empty = True,
    ),
    data = glob([
        "share/dotnet/host/**/*",
        "share/dotnet/sdk/{sdk_version}/FSharp/**/*.dll",
    ]) + [
        "share/dotnet/sdk/{sdk_version}/FSharp/fsc.runtimeconfig.json",
    ],
    visibility = ["//visibility:public"],
)

filegroup(
    name = "runtime",
    srcs = ["dotnet"],
    data = glob([
        "share/dotnet/host/**/*",
        "share/dotnet/sdk/**/*",
        "share/dotnet/shared/Microsoft.AspNetCore.App/{aspnet_version}/**/*",
        "share/dotnet/shared/Microsoft.NETCore.App/{runtime_version}/**/*",
    ]),
    visibility = ["//visibility:public"],
)

import_dll(
    name = "host_model",
    version = "{sdk_version}",
    dll = "share/dotnet/sdk/{sdk_version}/Microsoft.NET.HostModel.dll",
)

dotnet_toolchain(
    name = "dotnet_toolchain",
    runtime = ":runtime",
    csharp_compiler = ":csc_binary",
    fsharp_compiler = ":fsc_binary",
    host_model = ":host_model",
    sdk_version = "{sdk_version}",
    runtime_version = "{runtime_version}",
    runtime_tfm = "{runtime_tfm}",
    csharp_default_version = "{csharp_default_version}",
    fsharp_default_version = "{fsharp_default_version}",
    visibility = ["//visibility:public"],
)
""".format(
        sdk_version = sdk_version,
        runtime_version = runtime_version,
        aspnet_version = aspnet_version,
        runtime_tfm = runtime_tfm,
        csharp_default_version = csharp_default_version,
        fsharp_default_version = fsharp_default_version,
    )

    # `register_toolchains` can only pick up `toolchain()` declarations, not the raw
    # dotnet_toolchain rule; mirror what rules_dotnet's toolchains_repo.bzl does.
    # Constraint labels come in as attr.label values, so `str()` yields canonical
    # resolvable labels for the generated BUILD file.
    platforms = [
        ("x86_64-unknown-linux-gnu", repository_ctx.attr.linux_x86_64_constraints),
        ("arm64-unknown-linux-gnu", repository_ctx.attr.linux_aarch64_constraints),
        ("x86_64-apple-darwin", repository_ctx.attr.macos_x86_64_constraints),
        ("aarch64-apple-darwin", repository_ctx.attr.macos_aarch64_constraints),
    ]
    build_content += _toolchain_targets(platforms)
    repository_ctx.file("BUILD.bazel", content = build_content)

nix_dotnet_sdk_repository = repository_rule(
    implementation = _nix_dotnet_sdk_repository_impl,
    attrs = _ATTRIBUTES,
)
# Mostly Ai Generated - End
