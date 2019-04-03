#!/usr/bin/env bash

set -u -x

pushd SpatialOS

gtimeout 30s ./scripts/run_server.sh

status=$?

if [ $status -eq 124 ] #timed out
then
    exit 0
fi

exit $status

popd