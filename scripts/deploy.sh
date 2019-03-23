#!/usr/bin/env bash

set -e
set -u

spatial alpha cloud upload -a $ASSEMBLY_NAME

spatial alpha cloud launch -a $ASSEMBLY_NAME -d $DEPLOYMENT_NAME

spatial project deployment tags add $DEPLOYMENT_NAME $DEPLOYMENT_TTL
