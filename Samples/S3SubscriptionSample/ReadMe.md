![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp S3 Bucket Events Source

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Creating a function that is invoked by a S3 bucket events requires two steps. First, either create or reference an S3 bucket in the `Items` section. Second, the function must reference the bucket in its `Sources` section using the `S3` attribute.

Optionally, the `S3` attribute can specify specific [S3 events](https://docs.aws.amazon.com/AmazonS3/latest/dev/NotificationHowTo.html#notification-how-to-event-types-and-destinations) to listen to, an S3 key prefix and suffix.

```yaml
Module: Sample.S3.Subscription
Description: A sample module integrating with S3 Bucket events
Items:

  - Resource: MyFirstBucket
    Description: The S3 Bucket the function is listening to
    Type: AWS::S3::Bucket
    Allow: ReadWrite

  - Resource: MySecondBucket
    Description: The S3 Bucket the function is listening to
    Type: AWS::S3::Bucket
    Allow: ReadWrite

  - Function: MyFunction
    Description: This function is invoked by an S3 Bucket event
    Memory: 128
    Timeout: 30
    Sources:

      # listen to `s3:ObjectCreated:*` on the bucket
      - S3: MyFirstBucket

      # listen to custom events on specific S3 keys
      - S3: MySecondBucket
        Events:
          - "s3:ObjectCreated:*"
          - "s3:ObjectRemoved:*"
        Prefix: images/
        Suffix: .png
```

## Function Code

The S3 event can be parsed into a `S3Event` message instance by using the `ALambdaFunction<T>` base class and including the `Amazon.Lambda.S3Events` nuget package.

```csharp
public class Function : ALambdaFunction<S3Event> {

    //--- Methods ---
    public override Task InitializeAsync(LambdaConfig config)
        => Task.CompletedTask;

    public override async Task<object> ProcessMessageAsync(S3Event s3Event) {
        LogInfo($"# S3 Records = {s3Event.Records.Count}");
        for(var i = 0; i < s3Event.Records.Count; ++i) {
            var record = s3Event.Records[i];
            LogInfo($"EventName = {record.EventName.Value}");
            LogInfo($"EventSource = {record.EventSource}");
            LogInfo($"EventTime = {record.EventTime}");
            LogInfo($"EventVersion = {record.EventVersion}");
            LogInfo($"S3.Bucket.Name = {record.S3.Bucket.Name}");
            LogInfo($"S3.Object.ETag = {record.S3.Object.ETag}");
            LogInfo($"S3.Object.Key = {record.S3.Object.Key}");
            LogInfo($"S3.Object.Size = {record.S3.Object.Size}");
            LogInfo($"S3.Object.VersionId = {record.S3.Object.VersionId}");
            LogInfo($"UserIdentity.PrincipalId = {record.UserIdentity.PrincipalId}");
        }
        return "Ok";
    }
}
```
