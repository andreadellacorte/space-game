#!/usr/bin/env bash

set -e
set -u

pushd SpatialOS

spatial cloud delete $DEPLOYMENT_NAME || true

popd