#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi
set -x

# remove old zip file, just in case
rm BlazorProjectTemplate.zip > /dev/null 2>&1

# zip project without any build artifacts
cd BlazorAppTemplate
rm -rf bin obj > /dev/null 2>&1
zip -r ../BlazorProjectTemplate.zip *
cd ..

# update blazor project in tool
cp BlazorProjectTemplate.zip $LAMBDASHARP/src/LambdaSharp.Tool/Resources/.
rm BlazorProjectTemplate.zip > /dev/null 2>&1
