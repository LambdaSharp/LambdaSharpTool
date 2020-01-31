#!/bin/bash
if [ -z "$LAMBDASHARP" ]; then
    echo "ERROR: environment variable \$LAMBDASHARP is not set"
    exit 1
fi

# never use suffix for tests
unset LAMBDASHARP_VERSION_SUFFIX

if [ -z "$1" ]; then

    # run everything
    dotnet run -p $LAMBDASHARP/src/LambdaSharp.Tool/LambdaSharp.Tool.csproj --force -- info \
        --verbose:exceptions \
        --tier Test \
        --aws-region us-east-1 \
        --aws-account-id 123456789012 \
        --aws-user-arn arn:aws:iam::123456789012:user/test-user \
        --tier-version $LAMBDASHARP_VERSION_PREFIX \
        --cli-version $LAMBDASHARP_VERSION_PREFIX \
        --deployment-bucket-name lambdasharp-bucket-name

    if [ $? -ne 0 ]; then
        exit $?
    fi

    rm $LAMBDASHARP/Tests/Modules/Results/*.json > /dev/null 2>&1
    dotnet $LAMBDASHARP/src/LambdaSharp.Tool/bin/Debug/netcoreapp2.1/LambdaSharp.Tool.dll deploy \
        --verbose:exceptions \
        --tier Test \
        --cfn-output $LAMBDASHARP/Tests/Modules/Results/ \
        --dryrun:cloudformation \
        --aws-region us-east-1 \
        --aws-account-id 123456789012 \
        --aws-user-arn arn:aws:iam::123456789012:user/test-user \
        --git-sha 0123456789ABCDEF0123456789ABCDEF01234567 \
        --git-branch test-branch \
        --tier-version $LAMBDASHARP_VERSION_PREFIX \
        --cli-version $LAMBDASHARP_VERSION_PREFIX \
        --deployment-bucket-name lambdasharp-bucket-name \
        --no-dependency-validation \
        --module-build-date 20190809150000 \
        $LAMBDASHARP/Tests/Modules/Empty.yml \
        $LAMBDASHARP/Tests/Modules/Empty-NoLambdaSharpDependencies.yml \
        $LAMBDASHARP/Tests/Modules/Empty-NoModuleRegistration.yml \
        $LAMBDASHARP/Tests/Modules/Function.yml \
        $LAMBDASHARP/Tests/Modules/Function-NoLambdaSharpDependencies.yml \
        $LAMBDASHARP/Tests/Modules/Function-NoModuleRegistration.yml \
        $LAMBDASHARP/Tests/Modules/Function-NoFunctionRegistration.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Base64.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Cidr.yml \
        $LAMBDASHARP/Tests/Modules/Fn-FindInMap.yml \
        $LAMBDASHARP/Tests/Modules/Fn-GetAtt.yml \
        $LAMBDASHARP/Tests/Modules/Fn-GetAZs.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Include.yml \
        $LAMBDASHARP/Tests/Modules/Fn-ImportValue.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Join.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Ref.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Select.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Split.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Sub.yml \
        $LAMBDASHARP/Tests/Modules/Fn-Transform.yml \
        $LAMBDASHARP/Tests/Modules/Source-Topic.yml \
        $LAMBDASHARP/Tests/Modules/Source-Timer.yml \
        $LAMBDASHARP/Tests/Modules/Source-Api-SlackCommand.yml \
        $LAMBDASHARP/Tests/Modules/Source-Api-RequestResponse.yml \
        $LAMBDASHARP/Tests/Modules/Source-S3.yml \
        $LAMBDASHARP/Tests/Modules/Source-Sqs.yml \
        $LAMBDASHARP/Tests/Modules/Source-Alexa.yml \
        $LAMBDASHARP/Tests/Modules/Variables.yml \
        $LAMBDASHARP/Tests/Modules/Source-DynamoDB.yml \
        $LAMBDASHARP/Tests/Modules/Source-Kinesis.yml \
        $LAMBDASHARP/Tests/Modules/Parameter-String.yml \
        $LAMBDASHARP/Tests/Modules/Parameter-Resource.yml \
        $LAMBDASHARP/Tests/Modules/Parameter-ConditionalResource.yml \
        $LAMBDASHARP/Tests/Modules/Parameter-Secret.yml \
        $LAMBDASHARP/Tests/Modules/Import-String.yml \
        $LAMBDASHARP/Tests/Modules/Import-Resource.yml \
        $LAMBDASHARP/Tests/Modules/Import-Secret.yml \
        $LAMBDASHARP/Tests/Modules/Output-LiteralValue.yml \
        $LAMBDASHARP/Tests/Modules/Output-Variable.yml \
        $LAMBDASHARP/Tests/Modules/Output-Resource.yml \
        $LAMBDASHARP/Tests/Modules/Output-Function.yml \
        $LAMBDASHARP/Tests/Modules/Output-CustomResource.yml \
        $LAMBDASHARP/Tests/Modules/Output-Macro.yml \
        $LAMBDASHARP/Tests/Modules/Package.yml \
        $LAMBDASHARP/Tests/Modules/NestedModule.yml \
        $LAMBDASHARP/Tests/Modules/Variable-Secret.yml \
        $LAMBDASHARP/Tests/Modules/Function-Finalizer.yml \
        $LAMBDASHARP/Tests/Modules/Condition-Resource.yml \
        $LAMBDASHARP/Tests/Modules/Condition-Inline-Resource.yml \
        $LAMBDASHARP/Tests/Modules/Condition-Scoped-Resource.yml \
        $LAMBDASHARP/Tests/Modules/Condition-Function.yml \
        $LAMBDASHARP/Tests/Modules/Condition-Condition.yml \
        $LAMBDASHARP/Tests/BadModule \
        $LAMBDASHARP/Modules/LambdaSharp.Core \
        $LAMBDASHARP/Modules/LambdaSharp.S3.IO \
        $LAMBDASHARP/Modules/LambdaSharp.S3.Subscriber \
        $LAMBDASHARP/Modules/LambdaSharp.Twitter.Query \
        $LAMBDASHARP/Samples/AlexaSample \
        $LAMBDASHARP/Samples/ApiSample \
        $LAMBDASHARP/Samples/ApiInvokeSample \
        $LAMBDASHARP/Samples/CustomResourceTypeSample \
        $LAMBDASHARP/Samples/DynamoDBSample \
        $LAMBDASHARP/Samples/FinalizerSample \
        $LAMBDASHARP/Samples/KinesisSample \
        $LAMBDASHARP/Samples/MacroSample \
        $LAMBDASHARP/Samples/S3IOSample \
        $LAMBDASHARP/Samples/S3SubscriptionSample \
        $LAMBDASHARP/Samples/ScheduleSample \
        $LAMBDASHARP/Samples/SlackCommandSample \
        $LAMBDASHARP/Samples/SnsSample \
        $LAMBDASHARP/Samples/SqsFailureHandlingSample \
        $LAMBDASHARP/Samples/SqsSample \
        $LAMBDASHARP/Samples/VpcFunctionSample \
        $LAMBDASHARP/Samples/WebSocketSample \
        $LAMBDASHARP/Demos/DemoS3BucketSubscription/DemoS3Bucket \
        $LAMBDASHARP/Demos/DemoS3BucketSubscription/DemoS3Subscriber \
        $LAMBDASHARP/Demos/SlackTodo \
        $LAMBDASHARP/Demos/TwitterNotifier \

else
    testfile=$(basename $1 .yml)

    # run requested test
    rm $LAMBDASHARP/Tests/Modules/Results/$testfile.json > /dev/null 2>&1
    dotnet run -p $LAMBDASHARP/src/LambdaSharp.Tool/LambdaSharp.Tool.csproj --force -- deploy \
        --verbose:exceptions \
        --tier Test \
        --cfn-output $LAMBDASHARP/Tests/Modules/Results/$testfile.json \
        --dryrun:cloudformation \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --git-sha 0123456789ABCDEF0123456789ABCDEF01234567 \
        --git-branch test-branch \
        --tier-version $LAMBDASHARP_VERSION_PREFIX \
        --cli-version $LAMBDASHARP_VERSION_PREFIX \
        --deployment-bucket-name lambdasharp-bucket-name \
        --no-dependency-validation \
        --module-build-date 20190809150000 \
        $LAMBDASHARP/Tests/Modules/$1
fi
