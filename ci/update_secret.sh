#!/usr/bin/env bash

set -e -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"

TOKEN=ADD_OAUTH2_REFRESH_TOKEN_HERE

travis login --auto-token --com

echo $TOKEN > ./oauth2_refresh_token

tar cvf ./secrets.tar ./oauth2_refresh_token

travis encrypt-file secrets.tar -r andreadellacorte/space-game --com

mv ./secrets.tar.enc ..

rm ./oauth2_refresh_token
rm ./secrets.tar

popd