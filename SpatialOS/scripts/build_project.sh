#!/usr/bin/env bash

# This script builds the full project by running all other provided shell scripts in sequence

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"
source ./utils.sh

./download_dependencies.sh
./generate_schema_descriptor.sh

# Build all workers in the project
for WORKER_DIR in "${WORKER_DIRS[@]}"; do
  pushd "${WORKER_DIR}"
  ../SpatialOS/scripts/build_worker.sh
  popd
done

echo "Build complete."

popd