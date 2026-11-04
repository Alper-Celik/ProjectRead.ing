--- dotnet/private/rules/nuget/nuget_archive.bzl
+++ dotnet/private/rules/nuget/nuget_archive.bzl
@@ -253,18 +253,27 @@
         group["dotnet/{}".format(lang)].append(file)
     elif len(parts) == 5:
         lang = parts[3]
-        supported_architecture = parts[2]
+        # Path layout: analyzers/roslyn<M.m>/dotnet/<lang>/<file>.dll.
+        # The roslyn version lives in parts[1]; parts[2] is always "dotnet"
+        # and was compared by mistake in the original code, so only the first
+        # .dll of a folder survived.
+        roslyn_version = parts[1]
+        group_name = "dotnet/{}".format(lang)
+        lowest_group_name = group_name + "/lowest"
 
-        # Use this as a marker for the lowest version of the dll
-        if not group.get("dotnet/{}/lowest".format(lang)):
-            group["dotnet/{}/lowest".format(lang)] = file
-            group["dotnet/{}".format(lang)].append(file)
-        else:
-            # If the current version is lower than the version we have stored we replace it
-            current_lowest_version = group["dotnet/{}/lowest".format(lang)].split("/")[2]
-            if supported_architecture < current_lowest_version:
-                group["dotnet/{}/lowest".format(lang, supported_architecture)] = file
-                group["dotnet/{}".format(lang)].append(file)
+        # Keep the files of the HIGHEST available roslyn version (MSBuild
+        # picks the highest folder <= the compiler's Roslyn generation). The
+        # previous "lowest wins" handling shipped old-generation analyzer
+        # builds: e.g. Riok.Mapperly's roslyn4.0 build does not emit
+        # required-member assignments that its roslyn5.0 build does.
+        if lowest_group_name not in group:
+            group[lowest_group_name] = roslyn_version
+            group[group_name] = [file]
+        elif roslyn_version == group[lowest_group_name]:
+            group[group_name].append(file)
+        elif roslyn_version > group[lowest_group_name]:
+            group[lowest_group_name] = roslyn_version
+            group[group_name] = [file]
 
     return
 
