#!/usr/bin/env bash

set -e -u -x

pushd "$( dirname "${BASH_SOURCE[0]}" )"

# Write token to file
TOKEN=ADD_OAUTH2_REFRESH_TOKEN_HERE
echo $TOKEN > ./oauth2_refresh_token

# Tar secrets since travis can only encrypt one file at the time
tar cvf ./secrets.tar ./oauth2_refresh_token

# Sign in to travis
travis login --auto-token --com
travis encrypt-file secrets.tar -r andreadellacorte/space-game --com

# Cleanup
rm ./oauth2_refresh_token
rm ./secrets.tar

popd