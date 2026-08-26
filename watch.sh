#!/usr/bin/env sh
# For Everest hot-reloading, set CodeReload_WIP=true in modsettings-Everest.celeste
cd "$(dirname "$0")"
dotnet watch build --project Source "$@"