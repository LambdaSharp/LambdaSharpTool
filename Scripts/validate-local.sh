#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

# Validate λ# Release

cd $LAMBDASHARP

# Setup and validate λ# CLI
Scripts/install-cli.sh


echo "*******************************************"
echo "*** Update CloudFormation Specification ***"
echo "*******************************************"

lash util download-cloudformation-spec
if [ $? -ne 0 ]; then
    exit $?
fi

UNCOMMITTED=$(git status --porcelain 2>/dev/null| egrep "^(M| M)" | wc -l)
if [ $UNCOMMITTED -ne "0" ]; then
    echo "ERROR: found $UNCOMMITTED uncommitted files"
    exit 1
fi


echo "***********************************"
echo "*** Run Module Generation Tests ***"
echo "***********************************"

git update-index -q --refresh
if ! git diff-index --quiet HEAD -- Tests/; then
    git diff-index --name-only HEAD -- Tests/
    echo "ERROR: found changes in Tests/ folder BEFORE running tests"
    exit 1
fi

Scripts/run-tests.sh

git update-index -q --refresh
if ! git diff-index --quiet HEAD -- Tests/; then
    git diff-index --name-only HEAD -- Tests/
    echo "ERROR: found changes in Tests/ folder AFTER running tests"
    exit 1
fi


echo "**********************"
echo "*** Run Unit Tests ***"
echo "***********************"

dotnet test "$LAMBDASHARP/Tests/Tests.LambdaSharp"
if [ $? -ne 0 ]; then
    exit $?
fi

dotnet test "$LAMBDASHARP/Tests/Tests.LambdaSharp.Modules"
if [ $? -ne 0 ]; then
    exit $?
fi

dotnet test "$LAMBDASHARP/Modules/LambdaSharp.Core/Tests/ProcessLogEventsTests"
if [ $? -ne 0 ]; then
    exit $?
fi

dotnet test "$LAMBDASHARP/Modules/LambdaSharp.App.EventBus/Test.LambdaSharp.App.EventBus"
if [ $? -ne 0 ]; then
    exit $?
fi


echo "************************"
echo "*** Init LambdaSharp ***"
echo "*************************"

cd $LAMBDASHARP
SUFFIX=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 4 | head -n 1)
export LAMBDASHARP_TIER=TestContrib$SUFFIX

echo "Creating test tier: $LAMBDASHARP_TIER"
lash init \
    --quick-start \
    --core-services enabled \
    --verbose:exceptions
if [ $? -ne 0 ]; then
    exit $?
fi


echo "*********************"
echo "*** Build Samples ***"
echo "*********************"

cd $LAMBDASHARP/Samples
lash build `find . -name "Module.yml"`
if [ $? -ne 0 ]; then
    exit $?
fi


echo "********************"
echo "*** Build Demos ***"
echo "********************"

cd $LAMBDASHARP/Demos
lash build `find . -name "Module.yml"`
if [ $? -ne 0 ]; then
    exit $?
fi


echo "****************************"
echo "*** Build Legacy Modules ***"
echo "****************************"

cd $LAMBDASHARP/Tests/Legacy
lash build `find . -name "Module.yml"`
if [ $? -ne 0 ]; then
    exit $?
fi


# Deploy all λ# Sample Modules
echo "**********************"
echo "*** Deploy Samples ***"
echo "**********************"

cd $LAMBDASHARP
lash deploy  \
    --verbose:exceptions \
    Samples/AlexaSample/bin/cloudformation.json \
    Samples/ApiSample/bin/cloudformation.json \
    Samples/ApiInvokeSample/bin/cloudformation.json \
    Samples/ApiInvokeAssemblySample/bin/cloudformation.json \
    Samples/BlazorEventsSample/bin/cloudformation.json \
    Samples/BlazorSample/bin/cloudformation.json \
    Samples/CustomResourceTypeSample/bin/cloudformation.json \
    Samples/DynamoDBSample/bin/cloudformation.json \
    Samples/EventSample/bin/cloudformation.json \
    Samples/FinalizerSample/bin/cloudformation.json \
    Samples/JsonSerializerSample/bin/cloudformation.json \
    Samples/KinesisFirehoseSample/bin/cloudformation.json \
    Samples/KinesisSample/bin/cloudformation.json \
    Samples/LambdaLayerSample/bin/cloudformation.json \
    Samples/LambdaSelfContainedSample/bin/cloudformation.json \
    Samples/MacroSample/bin/cloudformation.json \
    Samples/MetricSample/bin/cloudformation.json \
    Samples/S3IOSample/bin/cloudformation.json \
    Samples/S3SubscriptionSample/bin/cloudformation.json \
    Samples/ScheduleSample/bin/cloudformation.json \
    Samples/SlackCommandSample/bin/cloudformation.json \
    Samples/SnsSample/bin/cloudformation.json \
    Samples/SqsSample/bin/cloudformation.json \
    Samples/WebSocketSample/bin/cloudformation.json

    # skipping this sample since it requires a VPC
    # Samples/VpcFunctionSample/bin/manifest.json

if [ $? -ne 0 ]; then
    exit $?
fi


# Create a Default λ# Module and Deploy it
echo "*****************************"
echo "*** Deploy Default Module ***"
echo "*****************************"

Scripts/test-new-module.sh
