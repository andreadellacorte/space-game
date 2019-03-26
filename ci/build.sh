#!/usr/bin/env bash

set -e -u -x

pushd "$( dirname "${BASH_SOURCE[0]}" )"

pushd ../SpatialOS

./scripts/build_project.sh

popd

pushd ../PlatformSDK

dotnet build

popd