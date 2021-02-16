#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

# mkdir TestModule
cd $LAMBDASHARP
if [ ! -d tmp ]; then
    mkdir tmp
fi
cd tmp
mkdir TestModule-$LAMBDASHARP_TIER
cd TestModule-$LAMBDASHARP_TIER

lash new module MyModule --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new app MyApp --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type apigateway MyApiGatewayFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type apigatewayproxy MyApiGatewayProxyFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type customresource MyCustomResourceFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type generic MyGenericFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type queue MyQueueFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type topic MyTopicFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type schedule MyScheduleFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type websocket MyWebSocketFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type websocketproxy MyWebSocketProxyFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type event MyEventFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --type selfcontained MySelfContainedFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function Finalizer --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash new function --language javascript MyJavascriptFunction --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

lash deploy --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi

cd ..
rm -rf TestModule-$LAMBDASHARP_TIER