#!/usr/bin/env bash

set -e -u

pushd SpatialOS

gtimeout 80s spatial alpha local launch

status=$?

if [ $status -eq 124 ] #timed out
then
    exit 0
fi
exit $status

popd