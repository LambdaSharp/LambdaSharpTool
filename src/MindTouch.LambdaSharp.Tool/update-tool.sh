rm *.nupkg

dotnet publish \
    --configuration NugetPublish

dotnet pack \
    --configuration NugetPublish \
    --output ./

dotnet nuget push \
    --api-key $LAMBDASHARP_NUGET_KEY \
    --source https://api.nuget.org/v3/index.json \
    `ls *.nupkg`
