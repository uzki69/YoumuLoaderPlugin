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

mkdir -p "$out"
cp "YoumuLoader/bin/Release/net9.0/publish/YoumuLoader.dll" "meta.json" "$out/"

# Update version and timestamp in meta.json
timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
jq --arg ver "$version" --arg ts "$timestamp" \
   '.version = $ver | .timestamp = $ts' "$out/meta.json" > "$out/meta.tmp.json"
mv "$out/meta.tmp.json" "$out/meta.json"

# Zip contents directly without parent folder wrapper
cd "$out"
zip -r "../$name.zip" ./*
cd ../..

rm -rf "$out"