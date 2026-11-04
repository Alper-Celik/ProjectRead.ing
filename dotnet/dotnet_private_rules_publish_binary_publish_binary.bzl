--- dotnet/private/rules/publish_binary/publish_binary.bzl
+++ dotnet/private/rules/publish_binary/publish_binary.bzl
@@ -181,6 +181,7 @@
         inputs = inputs,
         executable = copy_script,
         tools = [copy_script],
+        use_default_shell_env = True,
     )
 
     return (main_dll_copy, outputs, runfiles)
