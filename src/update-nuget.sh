# TODO:
# - check that LAMBDASHARP_NUGET_KEY is set
# - allow LAMBDASHARP_SUFFIX to be passed in

# Set version SUFFIX
LAMBDASHARP_SUFFIX=

update() {
    rm bin/Release/*.nupkg

    dotnet clean

    dotnet pack \
        --version-suffix "$LAMBDASHARP_SUFFIX" \
        --configuration Release

    dotnet nuget push \
        --api-key $LAMBDASHARP_NUGET_KEY \
        --source https://api.nuget.org/v3/index.json \
        `ls bin/Release/*.nupkg`
}

# Update MindTouch.LambdaSharp
cd MindTouch.LambdaSharp
update
cd ..

# Update MindTouch.LambdaSharp.CustomResource
cd MindTouch.LambdaSharp.CustomResource
update
cd ..

# Update MindTouch.LambdaSharp.Slack
cd MindTouch.LambdaSharp.Slack
update
cd ..
