#!/usr/bin/env bash

set -u

pushd SpatialOS

gtimeout 30s spatial alpha local launch

status=$?

if [ $status -eq 124 ] #timed out
then
    exit 0
fi

exit $status

popd