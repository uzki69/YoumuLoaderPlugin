#!/bin/bash
set -e

# Usage: ./update-manifest.sh <version> [repo_owner/repo_name] [tag_name]

VERSION="$1"
REPO="$2"
TAG="$3"

if [ -z "$VERSION" ]; then
  echo "Error: Version argument required."
  echo "Usage: $0 <version> [owner/repo] [tag]"
  exit 1
fi

ZIP_NAME="YoumuLoader_${VERSION}.zip"
ZIP_PATH="versions/${ZIP_NAME}"

if [ ! -f "$ZIP_PATH" ]; then
  echo "Error: Zip file $ZIP_PATH does not exist. Run publish.sh first."
  exit 1
fi

# Fallback values for local testing if repo/tag are not passed
REPO="${REPO:-uzki69/YoumuLoaderPlugin}"
TAG="${TAG:-v${VERSION}}"

# Construct the public download link for GitHub Releases
SOURCE_URL="https://github.com/${REPO}/releases/download/${TAG}/${ZIP_NAME}"

# Generate MD5 checksum and ISO-8601 UTC timestamp
CHECKSUM=$(md5sum "$ZIP_PATH" | awk '{ print $1 }')
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Append new version entry into manifest.json
jq --arg ver "$VERSION" \
   --arg url "$SOURCE_URL" \
   --arg md5 "$CHECKSUM" \
   --arg ts "$TIMESTAMP" \
   '.[0].versions += [{
     "version": $ver,
     "changelog": ("Release " + $ver),
     "targetAbi": "10.10.0.0",
     "sourceUrl": $url,
     "checksum": $md5,
     "timestamp": $ts
   }]' manifest.json > manifest.tmp.json

mv manifest.tmp.json manifest.json

echo "Successfully updated manifest.json for version $VERSION"