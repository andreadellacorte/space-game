#!/usr/bin/env bash

# This script runs the client script

set -e -u

mono --arch=64 ./Client.exe \"Client_$(openssl rand -hex 8)\"