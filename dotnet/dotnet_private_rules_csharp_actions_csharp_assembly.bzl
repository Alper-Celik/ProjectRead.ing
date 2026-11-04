--- dotnet/private/rules/csharp/actions/csharp_assembly.bzl
+++ dotnet/private/rules/csharp/actions/csharp_assembly.bzl
@@ -534,4 +534,5 @@
         env = {
             "DOTNET_CLI_HOME": toolchain.runtime.files_to_run.executable.dirname,
         },
+        use_default_shell_env = True,
     )
