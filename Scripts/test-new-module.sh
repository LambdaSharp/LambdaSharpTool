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
lash new module MyModule
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type apigateway MyApiGatewayFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type apigatewayproxy MyApiGatewayProxyFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type customresource MyCustomResourceFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type generic MyGenericFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type queue MyQueueFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type topic MyTopicFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type schedule MyScheduleFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type websocket MyWebSocketFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --type websocketproxy MyWebSocketProxyFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --language javascript MyJavascriptFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash deploy
if [ $? -ne 0 ]; then
    exit $?
fi
cd ..
rm -rf TestModule-$LAMBDASHARP_TIER