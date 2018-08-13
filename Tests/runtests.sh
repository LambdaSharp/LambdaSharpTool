dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- info \
    --tier test \
    --aws-account-id 123456789012 \
    --aws-region us-east-1 \
    --deployment-version 0.2 \
    --deployment-bucket-name lambdsharp-bucket-name \
    --deployment-deadletter-queue-url https://sqs.us-east-1.amazonaws.com/123456789012/LambdaSharp-DeadLetterQueue \
    --deployment-logging-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-LoggingTopic \
    --deployment-notification-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
    --deployment-rollbar-customresource-topic-arn arn:aws:sns:us-east-1:123456789012:LambdaSharpRollbar-RollbarCustomResourceTopic \
    --deployment-s3packageloader-customresource-topic-arn arn:aws:sns:us-east-1:123456789012:LambdaSharpRollbar-S3PackageLoaderCustomResourceTopic

lash() {
    dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- deploy \
        --tier Test \
        --output $1-CF.json \
        --dryrun:cloudformation \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --deployment-version 0.2 \
        --deployment-bucket-name lambdsharp-bucket-name \
        --deployment-deadletter-queue-url https://sqs.us-east-1.amazonaws.com/123456789012/LambdaSharp-DeadLetterQueue \
        --deployment-logging-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-LoggingTopic \
        --deployment-notification-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
        --deployment-rollbar-customresource-topic-arn arn:aws:sns:us-east-1:123456789012:LambdaSharpRollbar-RollbarCustomResourceTopic \
        --deployment-s3packageloader-customresource-topic-arn arn:aws:sns:us-east-1:123456789012:LambdaSharpRollbar-S3PackageLoaderCustomResourceTopic \
        $1.yml
}

lash Source-Topic
lash Source-Timer
lash Source-Api-SlackCommand
lash Source-Api-RequestResponse
lash Source-S3
lash Source-Sqs
lash Variables
lash Source-Alexa
