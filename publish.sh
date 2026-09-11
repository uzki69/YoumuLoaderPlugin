#!/bin/bash
set -e

# Use first argument if passed (CI), otherwise prompt for input (local)
if [ -n "$1" ]; then
  version="$1"
else
  echo "version number:"
  read version
fi

dotnet publish -c Release
name="YoumuLoader_$version"
out="./versions"
mkdir -p "$out/$name"
cd "$out"
cp "../YoumuLoader/bin/Release/net9.0/publish/YoumuLoader.dll" "../meta.json" "$name"

# Patch the version in meta.json inside the release folder
jq --arg ver "$version" '.version = $ver' "$name/meta.json" > "$name/meta.tmp.json"
mv "$name/meta.tmp.json" "$name/meta.json"

7z a "$name.zip" "$name"
rm -vr "$name"