#!/usr/bin/env bash

# This script runs the client script

set -e -x
pushd "$( dirname "${BASH_SOURCE[0]}" )"

mono --arch=64 ./bin/x64/ReleaseMacOS/SnapshotGenerator.exe ../SpatialOS/snapshots/default.snapshot

popd