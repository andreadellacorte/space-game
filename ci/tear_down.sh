#!/usr/bin/env bash

set -e -u

pushd SpatialOS

spatial cloud delete $DEPLOYMENT_NAME || true

popd