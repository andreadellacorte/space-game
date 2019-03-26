#!/usr/bin/env bash

set -e -u -x

pushd SpatialOS

spatial alpha cloud upload -a $ASSEMBLY_NAME

spatial alpha cloud launch -a $ASSEMBLY_NAME -d $DEPLOYMENT_NAME

spatial project deployment tags add $DEPLOYMENT_NAME $DEPLOYMENT_TTL

popd