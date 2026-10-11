#!/usr/bin/env bash
# Prints the public members of one generated game type (signatures only), without compiler-generated noise.
# Usage: tools/show-type.sh 'Il2Cpp.M_Player' [grep-filter]     (ASM=Unity.TextMeshPro to pick another assembly)
A="/c/Program Files (x86)/Steam/steamapps/common/Shroom and Gloom/MelonLoader/Il2CppAssemblies/${ASM:-Assembly-CSharp}.dll"
D="$(dirname "$0")/../reference/decomp"; mkdir -p "$D"
F="$D/$(echo "$1" | tr '`' '_').txt"
[ -s "$F" ] || ilspycmd -t "$1" "$A" 2>/dev/null | grep -E "^\s*(public|protected|namespace|[A-Za-z_]+( = -?[0-9]+)?,?$)" | grep -vE "IntPtr pointer\)|NativeFieldInfoPtr|NativeMethodInfoPtr|__|ObjectCompilerGenerated|System_Collections|System\.Collections\.|System_IDisposable|MoveNext|MethodInfoStoreGeneric|^\s*(get|set)$" | sed 's/^\s*//; s/unsafe //; s/virtual //' > "$F"
if [ -n "$2" ]; then grep -iE "$2" "$F"; else cat "$F"; fi
