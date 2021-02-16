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

# remove all bin/obj folders from previous builds
find "$LAMBDASHARP" -name 'bin' -or -name 'obj' | xargs rm -rf

# Update LambdaSharp.Logging
cd $LAMBDASHARP/src/LambdaSharp.Logging
update

# Update LambdaSharp
cd $LAMBDASHARP/src/LambdaSharp
update

# Update LambdaSharp.ApiGateway
cd $LAMBDASHARP/src/LambdaSharp.ApiGateway
update

# Update LambdaSharp.CloudWatch
cd $LAMBDASHARP/src/LambdaSharp.CloudWatch
update

# Update LambdaSharp.CustomResource
cd $LAMBDASHARP/src/LambdaSharp.CustomResource
update

# Update LambdaSharp.Finalizer
cd $LAMBDASHARP/src/LambdaSharp.Finalizer
update

# Update LambdaSharp.Schedule
cd $LAMBDASHARP/src/LambdaSharp.Schedule
update

# Update LambdaSharp.Serialization.NewtonsoftJson
cd $LAMBDASHARP/src/LambdaSharp.Serialization.NewtonsoftJson
update

# Update LambdaSharp.SimpleNotificationService
cd $LAMBDASHARP/src/LambdaSharp.SimpleNotificationService
update

# Update LambdaSharp.SimpleQueueService
cd $LAMBDASHARP/src/LambdaSharp.SimpleQueueService
update

# Update LambdaSharp.Slack
cd $LAMBDASHARP/src/LambdaSharp.Slack
update

# Update LambdaSharp.App
cd $LAMBDASHARP/src/LambdaSharp.App
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
