--- dotnet/private/common.bzl
+++ dotnet/private/common.bzl
@@ -770,6 +770,7 @@
             inputs = inputs,
             executable = copy_script,
             tools = [copy_script],
+            use_default_shell_env = True,
         )
     return outputs
 
