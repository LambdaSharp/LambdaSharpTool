if [ -z "$1" ]; then

    # run everything
    dotnet run -p $LAMBDASHARP/src/LambdaSharp.Tool/LambdaSharp.Tool.csproj -- info \
        --verbose:exceptions \
        --tier Test \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --core-version 0.5 \
        --cli-version 0.5 \
        --deployment-bucket-name lambdasharp-bucket-name \
        --deployment-notifications-topic  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
        --module-bucket-names registered-bucket-name,lambdasharp-bucket-name

    if [ $? -ne 0 ]; then
        exit $?
    fi

    rm Results/*.json > /dev/null 2>&1
    dotnet $LAMBDASHARP/src/LambdaSharp.Tool/bin/Debug/netcoreapp2.1/LambdaSharp.Tool.dll deploy \
        --verbose:exceptions \
        --tier Test \
        --cfn-output Results/ \
        --dryrun:cloudformation \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --git-sha 0123456789ABCDEF0123456789ABCDEF01234567 \
        --git-branch test-branch \
        --core-version 0.5 \
        --cli-version 0.5 \
        --deployment-bucket-name lambdasharp-bucket-name \
        --deployment-notifications-topic  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
        --module-bucket-names registered-bucket-name,lambdasharp-bucket-name \
        --no-dependency-validation \
        Empty.yml \
        Empty-NoLambdaSharpDependencies.yml \
        Empty-NoModuleRegistration.yml \
        Function.yml \
        Function-NoLambdaSharpDependencies.yml \
        Function-NoModuleRegistration.yml \
        Function-NoFunctionRegistration.yml \
        Fn-Base64.yml \
        Fn-Cidr.yml \
        Fn-FindInMap.yml \
        Fn-GetAtt.yml \
        Fn-GetAZs.yml \
        Fn-ImportValue.yml \
        Fn-Join.yml \
        Fn-Ref.yml \
        Fn-Select.yml \
        Fn-Split.yml \
        Fn-Sub.yml \
        Fn-Transform.yml \
        Source-Topic.yml \
        Source-Timer.yml \
        Source-Api-SlackCommand.yml \
        Source-Api-RequestResponse.yml \
        Source-S3.yml \
        Source-Sqs.yml \
        Source-Alexa.yml \
        Variables.yml \
        Source-DynamoDB.yml \
        Source-Kinesis.yml \
        Parameter-String.yml \
        Parameter-Resource.yml \
        Parameter-ConditionalResource.yml \
        Parameter-Secret.yml \
        Import-String.yml \
        Import-Resource.yml \
        Import-Secret.yml \
        Output-LiteralValue.yml \
        Output-Variable.yml \
        Output-Resource.yml \
        Output-Function.yml \
        Output-CustomResource.yml \
        Output-Macro.yml \
        Package.yml \
        NestedModule.yml \
        Variable-Secret.yml \
        Function-Finalizer.yml \
        Condition-Resource.yml \
        Condition-Inline-Resource.yml \
        Condition-Scoped-Resource.yml \
        Condition-Function.yml \
        Condition-Condition.yml \
        BadModule \
        ../Modules/LambdaSharp.Core \
        ../Modules/LambdaSharp.S3.IO \
        ../Modules/LambdaSharp.S3.Subscriber \
        ../Modules/LambdaSharp.Twitter.Query \
        ../Samples/AlexaSample \
        ../Samples/ApiSample \
        ../Samples/CustomResourceTypeSample \
        ../Samples/DynamoDBSample \
        ../Samples/FinalizerSample \
        ../Samples/KinesisSample \
        ../Samples/MacroSample \
        ../Samples/S3IOSample \
        ../Samples/S3SubscriptionSample \
        ../Samples/ScheduleSample \
        ../Samples/SlackCommandSample \
        ../Samples/SnsSample \
        ../Samples/SqsSample \
        ../Samples/VpcFunctionSample \
        ../Demos/DemoS3BucketSubscription/DemoS3Bucket \
        ../Demos/DemoS3BucketSubscription/DemoS3Subscriber \
        ../Demos/SlackTodo \
        ../Demos/TwitterNotifier \

else
    testfile=$(basename $1 .yml)

    # run requested test
    rm Results/$testfile.json > /dev/null 2>&1
    dotnet run -p $LAMBDASHARP/src/LambdaSharp.Tool/LambdaSharp.Tool.csproj -- deploy \
        --verbose:exceptions \
        --tier Test \
        --cfn-output Results/$testfile.json \
        --dryrun:cloudformation \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --git-sha 0123456789ABCDEF0123456789ABCDEF01234567 \
        --git-branch test-branch \
        --core-version 0.5 \
        --cli-version 0.5 \
        --deployment-bucket-name lambdasharp-bucket-name \
        --deployment-notifications-topic  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
        --no-dependency-validation \
        $1
fi
