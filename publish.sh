#!/bin/bash
set -e
echo "version number"
read version
dotnet publish
name="YoumuLoader_$version"
out="./versions"
mkdir -p "$out/$name"
cd "$out"
cp "../YoumuLoader/bin/Release/net9.0/publish/YoumuLoader.dll" "../meta.json" "$name"
7z a "$name.zip" "$name"
rm -vr "$name"
