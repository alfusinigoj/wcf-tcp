#!/bin/bash

set -o errexit
set -o pipefail
set -o nounset

source env.sh

mkdir -p app

cp WCFSelfHost/WCFSelfHost/bin/Debug/WCFSelfHost.exe app/

cf push $APP_NAME -p app/ -c WCFSelfHost.exe --no-start --no-route -b binary_buildpack -s windows2016 -u port 

GUID=$(cf app $APP_NAME --guid)

cf curl /v2/apps/$GUID -X PUT -d "{\"ports\":[$TCP_ROUTE_PORT]}"

cf map-route $APP_NAME $TCP_ROUTE_DOMAIN --port $TCP_ROUTE_PORT

cf start $APP_NAME