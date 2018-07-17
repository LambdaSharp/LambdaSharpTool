rm bin/Release/*.nupkg

dotnet pack \
    --configuration Release

dotnet nuget push \
    --api-key $LAMBDASHARP_NUGET_KEY \
    --source https://api.nuget.org/v3/index.json \
    `ls bin/Release/*.nupkg`