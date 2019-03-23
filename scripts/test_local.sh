#!/usr/bin/env bash

set -u

pushd SpatialOS

gtimeout 45s spatial local launch

status=$?

if [ $status -eq 124 ] #timed out
then
    exit 0
fi
exit $status

popd