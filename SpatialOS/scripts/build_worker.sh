#!/usr/bin/env bash

# This script builds the workers executable to folder/bin

set -e -u

source ../SpatialOS/scripts/utils.sh

# Download the dependenties in case they are not present
if [ ! -d "lib" ] || [ ! -d "${SCHEMA_DIR}/improbable" ]; then
  ../SpatialOS/scripts/download_dependencies.sh
fi

# Generate C# component API code from the project schema using the schema compiler
OUT_DIR="$(pwd)/src/improbable/generated"
mkdir -p "${OUT_DIR}"
"${TOOLS_DIR}"/schema_compiler/schema_compiler \
  --schema_path="${SCHEMA_DIR}" \
  --csharp_out="${OUT_DIR}" \
  --load_all_schema_on_schema_path \
  "${SCHEMA_DIR}"/*.schema \
  "${SCHEMA_DIR}"/improbable/*.schema

# Build a worker executable for each target build platform
for PLATFORM in "${BUILD_PLATFORMS[@]}"; do
  ${BUILD_TOOL} $(pwd)/src/CsharpWorker.sln /property:Configuration=Release /property:Platform="$PLATFORM" /verbosity:minimal
  cp -r $(pwd)/src/bin .
  rm -rf $(pwd)/src/bin
  rm -rf $(pwd)/src/obj
done