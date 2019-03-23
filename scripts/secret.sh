#!/usr/bin/env bash

set -e
set -u

openssl aes-256-cbc -K $encrypted_93597bdefda6_key -iv $encrypted_93597bdefda6_iv -in oauth2_refresh_token.enc -out oauth2_refresh_token -d
openssl aes-256-cbc -K $encrypted_93597bdefda6_key -iv $encrypted_93597bdefda6_iv -in oauth2_access_token_spatial_improbable_cli_client_go.enc -out oauth2_access_token_spatial_improbable_cli_client_go -d

mkdir -p ~/.improbable/oauth2

mv ./oauth2_refresh_token ~/.improbable/oauth2/oauth2_refresh_token
mv ./oauth2_access_token_spatial_improbable_cli_client_go ~/.improbable/oauth2/oauth2_access_token_spatial_improbable_cli_client_go