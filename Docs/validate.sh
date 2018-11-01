# Validate λ# Release

cd $LAMBDASHARP

# Run CloudFormation Generation Tests
# > running these tests should have no impact of any generated files

git update-index -q --refresh
if ! git diff-index --quiet HEAD -- Tests/; then
    git diff-index --name-only HEAD -- Tests/
    echo "ERROR: found changes in Tests/ folder BEFORE running tests"
    exit 1
fi

cd tests
rm *-CF.json
./runtests.sh
cd ..

git update-index -q --refresh
if ! git diff-index --quiet HEAD -- Tests/; then
    git diff-index --name-only HEAD -- Tests/
    echo "ERROR: found changes in Tests/ folder AFTER running tests"
    exit 1
fi

# Bootstrap the λ# Environment

lash() {
    dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- $*
}

SUFFIX=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 4 | head -n 1)
LAMBDASHARP_TIER=ReleaseTest$SUFFIX

lash init
if [ $? -ne 0 ]; then
    exit $?
fi

# Deploy the λ# Demos

lash deploy \
    Demos/Demo \
    Demos/BadModule
if [ $? -ne 0 ]; then
    exit $?
fi

# Deploy all λ# Sample Modules

lash build \
    Samples/AlexaSample \
    Samples/ApiSample \
    Samples/DynamoDBSample \
    Samples/KinesisSample \
    Samples/MacroSample \
    Samples/S3Sample \
    Samples/ScheduleSample \
    Samples/SlackCommandSample \
    Samples/SnsSample \
    Samples/SqsSample
if [ $? -ne 0 ]; then
    exit $?
fi

lash deploy \
    Samples/AlexaSample/bin/manifest.json \
    Samples/ApiSample/bin/manifest.json \
    Samples/DynamoDBSample/bin/manifest.json \
    Samples/KinesisSample/bin/manifest.json \
    Samples/MacroSample/bin/manifest.json \
    Samples/S3Sample/bin/manifest.json \
    Samples/ScheduleSample/bin/manifest.json \
    Samples/SlackCommandSample/bin/manifest.json \
    Samples/SnsSample/bin/manifest.json \
    Samples/SqsSample/bin/manifest.json

# Create a Default λ# Module and Deploy it

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
