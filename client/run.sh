#!/usr/bin/env bash

# This script runs the client script

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"
source ../SpatialOS/scripts/utils.sh

if [ "$PLATFORM_NAME" = "win32" ]; then
  start ./bin/x64/ReleaseWindows/Client.exe "Client_$(openssl rand -hex 8)" localhost 7777
fi

if [ "$PLATFORM_NAME" = "macOS64" ]; then
  mono --arch=64 ./bin/x64/ReleaseMacOS/Client.exe "Client_$(openssl rand -hex 8)" localhost 7777
fi

popd
