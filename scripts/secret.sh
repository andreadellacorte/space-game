#!/usr/bin/env bash

set -e
set -u

openssl aes-256-cbc -K $encrypted_6aee264528f1_key -iv $encrypted_6aee264528f1_iv -in secret.enc -out secret -d
mkdir -p ~/.improbable/oauth2
mv ./secret ~/.improbable/oauth2/oauth2_refresh_token
