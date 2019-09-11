#!/bin/bash

if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

if [ -z "$LAMBDASHARP_NUGET_KEY" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP_NUGET_KEY is not set"
    exit 1
fi

if [ -z "$LAMBDASHARP_VERSION" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP_VERSION is not set"
    exit 1
fi

read -p "Proceed with publishing v$LAMBDASHARP_VERSION? [y/n] " -n 1 -r
echo    # (optional) move to a new line
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    echo "Cancelled"
    exit 0
fi

update() {
    rm bin/Release/*.nupkg > /dev/null 2>&1

    dotnet clean

    dotnet pack \
        --configuration Release

    dotnet nuget push \
        --api-key $LAMBDASHARP_NUGET_KEY \
        --source https://api.nuget.org/v3/index.json \
        `ls bin/Release/*.nupkg`
}

# Update LambdaSharp
cd $LAMBDASHARP/src/LambdaSharp
update

# Update LambdaSharp.Slack
cd $LAMBDASHARP/src/LambdaSharp.Slack
update

# Update LambdaSharp.Tool
cd $LAMBDASHARP/src/LambdaSharp.Tool
rm *.nupkg > /dev/null 2>&1

dotnet publish \
    --configuration Release

dotnet pack \
    --configuration Release \
    --output ./

dotnet nuget push \
    --api-key $LAMBDASHARP_NUGET_KEY \
    --source https://api.nuget.org/v3/index.json \
    `ls *.nupkg`
