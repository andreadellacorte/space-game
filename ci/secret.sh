#!/usr/bin/env bash

set -e -u

openssl aes-256-cbc -K $encrypted_555b11e18606_key -iv $encrypted_555b11e18606_iv -in secrets.tar.enc -out secrets.tar -d

mkdir -p ~/.improbable/oauth2

tar xf secrets.tar

mv ./oauth2_refresh_token ~/.improbable/oauth2/oauth2_refresh_token
mv ./oauth2_access_token_spatial_improbable_cli_client_go ~/.improbable/oauth2/oauth2_access_token_spatial_improbable_cli_client_go