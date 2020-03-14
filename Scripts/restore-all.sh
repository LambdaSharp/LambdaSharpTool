#!/bin/bash

find "$LAMBDASHARP" -name '*.csproj' | xargs -l dotnet restore
