#!/bin/bash
set -euo pipefail

VERSION="${1:-}"
REPO="${2:-}"
TAG="${3:-}"

if [ -z "$VERSION" ]; then
  echo "Usage: $0 <version> [owner/repo] [tag]"
  exit 1
fi

# Keep this name in sync with publish.sh
ZIP_NAME="YoumuLoader_${VERSION}.zip"
ZIP_PATH="versions/${ZIP_NAME}"

if [ ! -f "$ZIP_PATH" ]; then
  echo "Error: $ZIP_PATH does not exist. Run publish.sh first."
  exit 1
fi

REPO="${REPO:-uzki69/YoumuLoader}"
TAG="${TAG:-v${VERSION}}"

SOURCE_URL="https://github.com/${REPO}/releases/download/${TAG}/${ZIP_NAME}"

CHECKSUM=$(md5sum "$ZIP_PATH" | awk '{print $1}')
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Safer jq: check structure and avoid duplicates
jq --arg ver "$VERSION" \
   --arg url "$SOURCE_URL" \
   --arg md5 "$CHECKSUM" \
   --arg ts "$TIMESTAMP" \
   '
   if type != "array" or length == 0 then
     error("manifest.json must be a non-empty array")
   else
     .[0].versions |= (
       map(select(.version != $ver)) + [{
         "version": $ver,
         "changelog": ("Release " + $ver),
         "targetAbi": "10.10.0.0",
         "sourceUrl": $url,
         "checksum": $md5,
         "timestamp": $ts
       }]
     )
   end
   ' manifest.json > manifest.tmp.json

mv manifest.tmp.json manifest.json
echo "Successfully updated manifest.json for version $VERSION"