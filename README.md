# Spike to enable WCF services with TCP bindings

1.) Create an `env.sh` file:

```
export APP_NAME=<desired CF app name>
export TCP_ROUTE_PORT=<desired CF tcp external port>
export TCP_ROUTE_DOMAIN=<your existing CF tcp domain>
```

2.) Build the solution with Visual Studio 2017

3.) Run `./up.sh`
