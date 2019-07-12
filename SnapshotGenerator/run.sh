#!/usr/bin/env bash

# This script runs the client script

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"
source ../SpatialOS/scripts/utils.sh

if [ "$PLATFORM_NAME" = "win32" ]; then
  ./bin/x64/ReleaseWindows/SnapshotGenerator.exe ../SpatialOS/snapshots/default.snapshot
fi

if [ "$PLATFORM_NAME" = "macOS64" ]; then
  mono --arch=64 ./bin/x64/ReleaseMacOS/SnapshotGenerator.exe ../SpatialOS/snapshots/default.snapshot
fi

popd
