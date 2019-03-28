#!/usr/bin/env bash

# This script runs the client script

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"

open -a Terminal ./run_game.sh

popd