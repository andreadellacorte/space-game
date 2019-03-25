#!/usr/bin/env bash

set -e
set -u

pushd "$( dirname "${BASH_SOURCE[0]}" )"

# travis login --com

cp ~/.improbable/oauth2/* .

tar cvf secrets.tar oauth2_access_token_spatial_improbable_cli_client_go oauth2_refresh_token

travis encrypt-file secrets.tar -r andreadellacorte/space-game --com

mv ./secrets.tar.enc ..

rm oauth2_access_token_spatial_improbable_cli_client_go
rm oauth2_refresh_token
rm secrets.tar

popd