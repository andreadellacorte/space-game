#!/usr/bin/env bash

set -e -u

pushd SpatialOS

./scripts/build_project.sh

popd

pushd PlatformSDK

dotnet build

popd