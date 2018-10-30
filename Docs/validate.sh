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

# Deploy the λ# Demos

lash deploy \
    Demos/Demo \
    Demos/BadModule

# Deploy all λ# Sample Modules

lash deploy \
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

# Create a Default λ# Module and Deploy it

# mkdir Test$SUFFIX
mkdir Test$SUFFIX
cd Test$SUFFIX
lash new module MyModule
lash new function MyFirstFunction
lash new function --language javascript MySecondFunction
lash deploy
cd ..

# TODO: delete test folder if deployment was successful
