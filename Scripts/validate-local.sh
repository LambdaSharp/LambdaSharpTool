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

Scripts/runtests.sh

git update-index -q --refresh
if ! git diff-index --quiet HEAD -- Tests/; then
    git diff-index --name-only HEAD -- Tests/
    echo "ERROR: found changes in Tests/ folder AFTER running tests"
    exit 1
fi


echo "**********************"
echo "*** Run Unit Tests ***"
echo "***********************"

cd $LAMBDASHARP/Tests/Tests.LambdaSharp
dotnet test
if [ $? -ne 0 ]; then
    exit $?
fi

cd $LAMBDASHARP/Tests/Tests.LambdaSharp.Tool
dotnet test
if [ $? -ne 0 ]; then
    exit $?
fi

cd $LAMBDASHARP/Modules/LambdaSharp.Core/Tests/ProcessLogEventsTests
dotnet test
if [ $? -ne 0 ]; then
    exit $?
fi

echo "************************"
echo "*** Init LambdaSharp ***"
echo "*************************"

cd $LAMBDASHARP
SUFFIX=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 4 | head -n 1)
LAMBDASHARP_TIER=TestContrib$SUFFIX

lash init \
    --core-services enabled \
    --existing-s3-bucket-name="" \
    --parameters $LAMBDASHARP/Scripts/lash-init-parameters.yml
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

# Deploy all λ# Sample Modules
echo "**********************"
echo "*** Deploy Samples ***"
echo "**********************"

cd $LAMBDASHARP
lash deploy \
    Samples/AlexaSample/bin/cloudformation.json \
    Samples/ApiSample/bin/cloudformation.json \
    Samples/CustomResourceTypeSample/bin/cloudformation.json \
    Samples/DynamoDBSample/bin/cloudformation.json \
    Samples/FinalizerSample/bin/cloudformation.json \
    Samples/KinesisSample/bin/cloudformation.json \
    Samples/LambdaLayerSample/bin/cloudformation.json \
    Samples/MacroSample/bin/cloudformation.json \
    Samples/S3IOSample/bin/cloudformation.json \
    Samples/S3SubscriptionSample/bin/cloudformation.json \
    Samples/ScheduleSample/bin/cloudformation.json \
    Samples/SlackCommandSample/bin/cloudformation.json \
    Samples/SnsSample/bin/cloudformation.json \
    Samples/SqsSample/bin/cloudformation.json

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
