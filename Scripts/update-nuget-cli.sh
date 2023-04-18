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

read --project "Proceed with publishing v$LAMBDASHARP_VERSION? [y/n] " -n 1 -r
echo    # (optional) move to a new line
if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    echo "Cancelled"
    exit 0
fi

# remove all bin/obj folders from previous builds
find "$LAMBDASHARP" -name 'bin' -or -name 'obj' | xargs rm -rf

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
