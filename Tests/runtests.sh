lash() {
    dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- deploy \
        --verbose:exceptions \
        --tier Test \
        --cf-output $1-CF.json \
        --dryrun:cloudformation \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --deployment-version 0.4 \
        --deployment-bucket-name lambdsharp-bucket-name \
        --deployment-notification-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
        $1.yml
}

if [ -z "$1" ]; then

    # run everything
    dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- info \
        --tier test \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --deployment-version 0.4 \
        --deployment-bucket-name lambdsharp-bucket-name \
        --deployment-notification-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic

    if [ $? -ne 0 ]; then
        exit $?
    fi
    lash Source-Topic
    lash Source-Timer
    lash Source-Api-SlackCommand
    lash Source-Api-RequestResponse
    lash Source-S3
    lash Source-Sqs
    lash Source-Alexa
    lash Parameters
    lash CloudFormationFunctions
    lash Source-DynamoDB
    lash Source-Kinesis
    lash Source-Macro
else

    # run requested test
    lash "$1"
fi
