--- dotnet/private/compiler_wrapper.sh
+++ dotnet/private/compiler_wrapper.sh
@@ -10,7 +10,7 @@
 PATHMAP_FLAG="-pathmap"
 
 # Needed because unfortunately the F# compiler uses a different flag name
-if [[ $(basename "$COMPILER") == "fsc.dll" ]]; then
+if [[ "${COMPILER##*/}" == "fsc.dll" ]]; then
   PATHMAP_FLAG="--pathmap"
 fi
 PATHMAP="$PATHMAP_FLAG:$PWD=."
