#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

find "$LAMBDASHARP" -name 'bin' -or -name 'obj' | xargs rm -rf

find . -name "*.csproj" -print0 | while read -d $'\0' file
do
    echo "Building: $file"
    dotnet build "$file"
    if [ $? -ne 0 ]; then
        exit $?
    fi
done
