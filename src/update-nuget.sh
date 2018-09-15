# TODO:
# - check LAMBDASHARP_NUGET_KEY is set
# - allow SUFFIX to be passed in

# Set version SUFFIX
SUFFIX=RC3

update() {
    rm bin/Release/*.nupkg

    dotnet pack \
        --version-suffix $SUFFIX \
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
