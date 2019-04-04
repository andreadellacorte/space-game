#!/usr/bin/env bash

# This script runs the client script

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"

mono --arch=64 ./Client.exe \"Client_$(openssl rand -hex 8)\"

popd