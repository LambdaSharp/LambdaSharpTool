lash() {
    rm $1-CF.json > /dev/null 2>&1
    dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- deploy \
        --verbose:exceptions \
        --tier Test \
        --cf-output $1-CF.json \
        --dryrun:cloudformation \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --tier-version 0.4 \
        --cli-version 0.4 \
        --deployment-bucket-name lambdasharp-bucket-name \
        --deployment-notifications-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
        $1.yml
}

if [ -z "$1" ]; then

    # run everything
    dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- info \
        --verbose:exceptions \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --tier Test \
        --tier-version 0.4 \
        --cli-version 0.4 \
        --deployment-bucket-name lambdasharp-bucket-name \
        --deployment-notifications-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic

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
    lash Variables
    lash CloudFormationFunctions
    lash Source-DynamoDB
    lash Source-Kinesis
    lash Inputs
    lash Outputs
    lash Package
else

    # run requested test
    lash "$1"
fi
