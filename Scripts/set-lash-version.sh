#!/bin/bash

export LAMBDASHARP_VERSION_PREFIX=0.7.0.12
export LAMBDASHARP_VERSION_SUFFIX=

# create full version text
if [ -z "$LAMBDASHARP_VERSION_SUFFIX" ]; then
    export LAMBDASHARP_VERSION=$LAMBDASHARP_VERSION_PREFIX
else
    export LAMBDASHARP_VERSION=$LAMBDASHARP_VERSION_PREFIX-$LAMBDASHARP_VERSION_SUFFIX
fi
