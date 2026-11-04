--- dotnet/private/rules/fsharp/actions/fsharp_assembly.bzl
+++ dotnet/private/rules/fsharp/actions/fsharp_assembly.bzl
@@ -455,4 +455,5 @@
         env = {
             "DOTNET_CLI_HOME": toolchain.runtime.files_to_run.executable.dirname,
         },
+        use_default_shell_env = True,
     )
