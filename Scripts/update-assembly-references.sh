#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

# ensure latest version of dotnet-outdated
# https://github.com/dotnet-outdated/dotnet-outdated
dotnet tool update --global dotnet-outdated-tool

# Exclude some assemblies that have breaking changes
EXCLUDE="--exclude Blazorise --exclude McMaster.Extensions.CommandLineUtils --exclude NJsonSchema --exclude YamlDotNet"

# find all C# projects, except those in the Tests/Legacy folder
dotnet outdated --upgrade --recursive "$LAMBDASHARP/Libraries" $EXCLUDE
dotnet outdated --upgrade --recursive "$LAMBDASHARP/src" $EXCLUDE
dotnet outdated --upgrade --recursive "$LAMBDASHARP/Modules" $EXCLUDE
dotnet outdated --upgrade --recursive "$LAMBDASHARP/Samples" $EXCLUDE
dotnet outdated --upgrade --recursive "$LAMBDASHARP/Demos" $EXCLUDE

find "$LAMBDASHARP/Tests" -name "*.csproj" -not -path "$LAMBDASHARP/Tests/Legacy/*" | xargs -I {} dotnet outdated --upgrade "{}" $EXCLUDE
