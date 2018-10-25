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

lash deploy \
    $LAMBDASHARP/Bootstrap/LambdaSharp/Module.yml \
    $LAMBDASHARP/Bootstrap/LambdaSharpRegistrar/Module.yml \
    $LAMBDASHARP/Bootstrap/LambdaSharpS3PackageLoader/Module.yml \
    $LAMBDASHARP/Bootstrap/LambdaSharpS3Subscriber/Module.yml

# Deploy the λ# Demo Module

lash deploy Demos/Demo/Module.yml

# Deploy all λ# Sample Modules

lash deploy \
    Samples/AlexaSample/Module.yml \
    Samples/ApiSample/Module.yml \
    Samples/DynamoDBSample/Module.yml \
    Samples/KinesisSample/Module.yml \
    Samples/MacroSample/Module.yml \
    Samples/S3Sample/Module.yml \
    Samples/ScheduleSample/Module.yml \
    Samples/SlackCommandSample/Module.yml \
    Samples/SnsSample/Module.yml \
    Samples/SqsSample/Module.yml

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
