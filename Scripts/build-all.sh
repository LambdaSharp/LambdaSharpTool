#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

find "$LAMBDASHARP" -name 'bin' -or -name 'obj' | xargs rm -rf
cd "$LAMBDASHARP"
find . -name "*.csproj" -print0 | while read -d $'\0' file
do
    if [[ $file =~ ^./Resources/* ]]; then
        echo "Skipping: $file"
    else
        echo "Building: $file"
        dotnet build "$file"
        if [ $? -ne 0 ]; then
            exit $?
        fi
    fi
done
