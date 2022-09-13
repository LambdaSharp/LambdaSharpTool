#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

# ensure latest version of dotnet-outdated
# https://github.com/dotnet-outdated/dotnet-outdated
dotnet tool update --global dotnet-outdated-tool

# find all C# projects, except those in the Tests/Legacy folder
dotnet outdated --upgrade --recursive "$LAMBDASHARP/Libraries"
dotnet outdated --upgrade --recursive "$LAMBDASHARP/src"
dotnet outdated --upgrade --recursive "$LAMBDASHARP/Modules"
dotnet outdated --upgrade --recursive "$LAMBDASHARP/Samples"
dotnet outdated --upgrade --recursive "$LAMBDASHARP/Demos"

find "$LAMBDASHARP/Tests" -name "*.csproj" -not -path "$LAMBDASHARP/Tests/Legacy/*" | xargs -I {} dotnet outdated --upgrade "{}"
