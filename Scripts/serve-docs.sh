#!/bin/bash

if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

if [ -z "$LAMBDASHARP_VERSION" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP_VERSION is not set"
    exit 1
fi

# write current version to `version.txt`, which is used by docfx
echo "*** WRITING VERSION TO FILE: $LAMBDASHARP_VERSION"
echo $LAMBDASHARP_VERSION > $LAMBDASHARP/Docs/version.txt

# generate new documentation
echo "*** SERVING DOCUMENTATION (HIT ENTER TO STOP)"
cd $LAMBDASHARP/Docs
docfx docfx.json --serve
