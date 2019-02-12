# Validate λ# Release

cd $LAMBDASHARP

# Run CloudFormation Generation Tests
# > running these tests should have no impact of any generated files

echo "*****************"
echo "*** Run Tests ***"
echo "*****************"

git update-index -q --refresh
if ! git diff-index --quiet HEAD -- Tests/; then
    git diff-index --name-only HEAD -- Tests/
    echo "ERROR: found changes in Tests/ folder BEFORE running tests"
    exit 1
fi

cd Tests
rm Results/*.json
./runtests.sh
cd ..

git update-index -q --refresh
if ! git diff-index --quiet HEAD -- Tests/; then
    git diff-index --name-only HEAD -- Tests/
    echo "ERROR: found changes in Tests/ folder AFTER running tests"
    exit 1
fi


echo "************************"
echo "*** Init LambdaSharp ***"
echo "*************************"

# Setup λ# in Contributor Mode

lash() {
    dotnet run -p $LAMBDASHARP/src/LambdaSharp.Tool/LambdaSharp.Tool.csproj -- $*
}

SUFFIX=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 4 | head -n 1)
LAMBDASHARP_TIER=TestContrib$SUFFIX

lash init
if [ $? -ne 0 ]; then
    exit $?
fi

# Deploy the λ# Demos
echo "********************"
echo "*** Deploy Demos ***"
echo "********************"

lash deploy \
    Demos/StaticWebsite \
    Demos/SlackTodo
if [ $? -ne 0 ]; then
    exit $?
fi

# Deploy all λ# Sample Modules
echo "*********************"
echo "*** Build Samples ***"
echo "*********************"

lash build \
    Samples/AlexaSample \
    Samples/ApiSample \
    Samples/CustomResourceTypeSample \
    Samples/DynamoDBSample \
    Samples/FinalizerSample \
    Samples/KinesisSample \
    Samples/LambdaLayerSample \
    Samples/MacroSample \
    Samples/S3IOSample \
    Samples/S3SubscriptionSample \
    Samples/ScheduleSample \
    Samples/SlackCommandSample \
    Samples/SnsSample \
    Samples/SqsSample \
    Samples/VpcFunctionSample

if [ $? -ne 0 ]; then
    exit $?
fi

echo "**********************"
echo "*** Deploy Samples ***"
echo "**********************"

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

# mkdir Test$SUFFIX
mkdir Test$SUFFIX
cd Test$SUFFIX
lash new module MyModule
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function MyFirstFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash new function --language javascript MySecondFunction
if [ $? -ne 0 ]; then
    exit $?
fi
lash deploy
if [ $? -ne 0 ]; then
    exit $?
fi
cd ..
rm -rf Test$SUFFIX
