#!/usr/bin/env sh
cd "$(dirname "$0")"
version=$(sed -n 's/^ *Version: *//p' everest.yaml | head -n 1 | tr -d '\r')
dotnet build Source "$@" -c Release -p:ZipName="ClimbTrials-v$version.zip"
