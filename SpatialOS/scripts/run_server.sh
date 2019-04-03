#!/usr/bin/env bash

# This script runs a local build of the project; you'll need to compile first

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"

spatial alpha local launch

popd