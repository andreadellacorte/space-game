#!/usr/bin/env bash

# This script runs the client script

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"

mono --arch=64 ./bin/x64/ReleaseMacOS/Client.exe localhost 7777 "Client_$(openssl rand -hex 8)"

popd