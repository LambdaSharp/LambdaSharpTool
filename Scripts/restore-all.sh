#!/bin/bash

find "$LAMBDASHARP" -name 'bin' -or -name 'obj' | xargs rm -rf
find "$LAMBDASHARP" -name '*.csproj' | xargs -l dotnet restore
