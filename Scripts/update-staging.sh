#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

# remove all bin/obj folders from previous builds
find "$LAMBDASHARP" -name 'bin' -or -name 'obj' | xargs rm -rf

# unset any environment variables we don't want to accidentally inherit
unset AWS_PROFILE
unset AWS_ACCESS_KEY_ID
unset AWS_SECRET_ACCESS_KEY
unset LAMBDASHARP_TIER
unset LAMBDASHARP_FEATURE_CACHING

# set the LambdaSharp version
source $LAMBDASHARP/scripts/set-lash-version.sh

# compile and install latest version
$LAMBDASHARP/Scripts/install-cli.sh

# update deployment tier
lash init \
    --allow-upgrade \
    --tier=Staging \
    --aws-profile=lambdasharp

# publish LambdaSharp standard modules
lash publish \
    --tier=Staging \
    --aws-profile=lambdasharp \
    --verbose:exceptions \
    --force-publish \
    --force-build \
    --module-version $LAMBDASHARP_VERSION \
    $LAMBDASHARP/Modules/LambdaSharp.Core \
    $LAMBDASHARP/Modules/LambdaSharp.S3.IO \
    $LAMBDASHARP/Modules/LambdaSharp.S3.Subscriber \
    $LAMBDASHARP/Modules/LambdaSharp.Twitter.Query
