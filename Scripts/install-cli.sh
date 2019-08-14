#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

if [ -z "$LAMBDASHARP_VERSION" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP_VERSION is not set"
    exit 1
fi

cd $LAMBDASHARP/src/LambdaSharp.Tool

rm *.nupkg

dotnet publish \
    --configuration Release

dotnet pack \
    --configuration Release \
    --output ./

dotnet tool uninstall \
    --global \
    LambdaSharp.Tool \

dotnet tool install \
    --global \
    --add-source ./ \
    LambdaSharp.Tool \
    --version $LAMBDASHARP_VERSION

# confirm the correct version was compiled
if ! (lash | grep -q "LambdaSharp CLI (v${LAMBDASHARP_VERSION})"); then
    echo "ERROR: MSBUILD output doesn't have the expected version number; expected 'LambdaSharp CLI (v${LAMBDASHARP_VERSION})'"
    dotnet tool uninstall \
        --global \
        LambdaSharp.Tool \
    exit 1
fi
