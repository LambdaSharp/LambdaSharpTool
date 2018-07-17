dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- info \
    --deployment test \
    --aws-account-id 123456789012 \
    --aws-region us-east-1 \
    --deployment-bucket-name lambdsharp-bucket-name \
    --deployment-deadletter-queue-url https://sqs.us-east-1.amazonaws.com/123456789012/LambdaSharp-DeadLetterQueue \
    --deployment-notification-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
    --deployment-rollbar-customresource-topic-arn arn:aws:sns:us-east-1:123456789012:LambdaSharpRollbar-RollbarCustomResourceTopic

lst() {
    dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj -- deploy \
        --deployment test \
        --input $1.yml \
        --output $1-CF.json \
        --dryrun:cloudformation \
        --bootstrap \
        --aws-account-id 123456789012 \
        --aws-region us-east-1 \
        --deployment-bucket-name lambdsharp-bucket-name \
        --deployment-deadletter-queue-url https://sqs.us-east-1.amazonaws.com/123456789012/LambdaSharp-DeadLetterQueue \
        --deployment-notification-topic-arn  arn:aws:sns:us-east-1:123456789012:LambdaSharp-DeploymentNotificationTopic \
        --deployment-rollbar-customresource-topic-arn arn:aws:sns:us-east-1:123456789012:LambdaSharpRollbar-RollbarCustomResourceTopic
}

lst Source-Topic
lst Source-Timer
lst Source-Api-SlackCommand
lst Source-Api-RequestResponse
lst Source-S3
lst Source-Sqs
lst Variables
