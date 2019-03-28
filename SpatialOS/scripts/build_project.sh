#!/usr/bin/env bash

# This script builds the full project by running all other provided shell scripts in sequence

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"
source ./utils.sh

./download_dependencies.sh
./generate_schema_descriptor.sh

echo $1

if [ $1 != "empty" ]; then
  WORKER_DIRS=($1)
fi

# Build all workers in the project
for WORKER_DIR in "${WORKER_DIRS[@]}"; do
  pushd "${WORKER_DIR}"
  if [ $# -gt 1 ]; then
    ../SpatialOS/scripts/build_worker.sh $2
  else
    ../SpatialOS/scripts/build_worker.sh
  fi
  popd
done

echo "Regenerating snapshot."
../../SnapshotGenerator/run.sh

popd

echo "Build complete."