#!/usr/bin/env bash

set -e -u -x

pushd SpatialOS

spatial cloud delete $DEPLOYMENT_NAME || true

popd