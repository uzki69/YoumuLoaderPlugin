#!/bin/bash
set -e

if [ -n "$1" ]; then
  version="$1"
else
  echo "version number:"
  read version
fi

dotnet publish -c Release
name="YoumuLoader_$version"
out="./versions/$name"

# Create clean output directory
mkdir -p "$out"

# Copy ONLY the target files into the directory
cp "YoumuLoader/bin/Release/net9.0/publish/YoumuLoader.dll" "meta.json" "$out/"

# Patch version inside meta.json
jq --arg ver "$version" '.version = $ver' "$out/meta.json" > "$out/meta.tmp.json"
mv "$out/meta.tmp.json" "$out/meta.json"