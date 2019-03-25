#!/bin/bash

set -o errexit
set -o pipefail
set -o nounset

source env.sh

cf delete -f $APP_NAME
cf delete-route $TCP_ROUTE_DOMAIN --port $TCP_ROUTE_PORT -f
