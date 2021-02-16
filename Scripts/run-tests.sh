#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

VERSION_PREFIX="1.0.0"

if [ -z "$1" ]; then

    # run everything
    dotnet run -p $LAMBDASHARP/src/LambdaSharp.Tool/LambdaSharp.Tool.csproj --force -- info \
        --verbose:exceptions \
        --no-beep \
        --tier Test \
        --aws-region us-east-1 \
        --aws-account-id 123456789012 \
        --aws-user-arn arn:aws:iam::123456789012:user/test-user \
        --tier-version $VERSION_PREFIX \
        --cli-version $VERSION_PREFIX \
        --deployment-bucket-name lambdasharp-bucket-name

    if [ $? -ne 0 ]; then
        exit $?
    fi

    # delete only generated output files
    find $LAMBDASHARP/Tests/Modules/ -maxdepth 1 -name *.yml | xargs -l basename | sed 's/.yml/.json/' | xargs -I{} rm $LAMBDASHARP/Tests/Modules/Results/{} > /dev/null 2>&1
    dotnet $LAMBDASHARP/src/LambdaSharp.Tool/bin/Debug/net5.0/LambdaSharp.Tool.dll deploy \
        --verbose:exceptions \
        --no-beep \
        --tier Test \
        --cfn-output $LAMBDASHARP/Tests/Modules/Results/ \
        --dryrun:cloudformation \
        --aws-region us-east-1 \
        --aws-account-id 123456789012 \
        --aws-user-arn arn:aws:iam::123456789012:user/test-user \
        --git-sha 0123456789ABCDEF0123456789ABCDEF01234567 \
        --git-branch test-branch \
        --tier-version $VERSION_PREFIX \
        --cli-version $VERSION_PREFIX \
        --deployment-bucket-name lambdasharp-bucket-name \
        --no-dependency-validation \
        --module-build-date 20190809150000 \
        `find $LAMBDASHARP/Tests/Modules/ -maxdepth 1 -name *.yml`

else
    testfile=$(basename $1 .yml)

    # run requested test
    rm $LAMBDASHARP/Tests/Modules/Results/$testfile.json > /dev/null 2>&1
    dotnet run -p $LAMBDASHARP/src/LambdaSharp.Tool/LambdaSharp.Tool.csproj --force -- deploy \
        --verbose:exceptions \
        --no-beep \
        --tier Test \
        --cfn-output $LAMBDASHARP/Tests/Modules/Results/$testfile.json \
        --dryrun:cloudformation \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --git-sha 0123456789ABCDEF0123456789ABCDEF01234567 \
        --git-branch test-branch \
        --tier-version $VERSION_PREFIX \
        --cli-version $VERSION_PREFIX \
        --deployment-bucket-name lambdasharp-bucket-name \
        --no-dependency-validation \
        --module-build-date 20190809150000 \
        $LAMBDASHARP/Tests/Modules/$1
fi
