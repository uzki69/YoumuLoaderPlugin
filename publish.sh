#!/bin/bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version>"
  exit 1
fi

PROJECT_DIR="YoumuLoader"          # ← adjust if needed
DLL_NAME="YoumuLoader.dll"
ZIP_NAME="YoumuLoader_${VERSION}.zip"   # keep consistent

dotnet publish -c Release -p:Version="$VERSION"

OUT_DIR="./versions/${ZIP_NAME%.zip}"
mkdir -p "$OUT_DIR"

cp "${PROJECT_DIR}/bin/Release/net9.0/publish/${DLL_NAME}" meta.json "$OUT_DIR/"

timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
jq --arg ver "$VERSION" --arg ts "$timestamp" \
   '.version = $ver | .timestamp = $ts' "$OUT_DIR/meta.json" > "$OUT_DIR/meta.tmp.json"
mv "$OUT_DIR/meta.tmp.json" "$OUT_DIR/meta.json"

(
  cd "$OUT_DIR"
  zip -r "../${ZIP_NAME}" ./*
)

rm -rf "$OUT_DIR"
echo "Created versions/${ZIP_NAME}"