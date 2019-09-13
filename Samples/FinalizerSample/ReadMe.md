![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp Module Finalizer

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

A LambdaSharp module can have a `Finalizer` function that is invoked as the last step of a CloudFormation stack creation and the first step of a CloudFormation stack deletion.

The `Finalizer` is useful to clean resources before CloudFormation attempts to delete them. Similarly, it can also be used to delete resources that were created dynamically during the lifetime of the module.

In this example, the `Finalizer` is used to delete all objects from an S3 bucket. Once deleted, the function returns, which allows CloudFormation to continue its clean-up operations, including deleting the S3 bucket.

```yaml
Module: Sample.Finalizer
Description: A sample module with a finalizer function
Items:

  - Resource: MyBucket
    Description: A sample resource being created before the finalizer is invoked
    Scope: Finalizer
    Type: AWS::S3::Bucket
    Allow: ReadWrite

  - Function: Finalizer
    Description: This function is invoked once all other resources have been created/updated
    Memory: 128
    Timeout: 600
```

## Function Code

The S3 event can be parsed into a `S3Event` message instance by using the `ALambdaFunction<T>` base class and including the `Amazon.Lambda.S3Events` nuget package.

```csharp
public class Function : ALambdaFinalizerFunction {

    //--- Fields ---
    private IAmazonS3 _s3Client;
    private string _bucketName;

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {
        _s3Client = new AmazonS3Client();

        // read configuration settings
        _bucketName = config.ReadS3BucketName("MyBucket");
    }

    public override async Task CreateDeployment(FinalizerProperties current) {
        LogInfo($"Creating Deployment: {current.DeploymentChecksum}");
    }

    public override async Task UpdateDeployment(FinalizerProperties next, FinalizerProperties previous) {
        LogInfo($"Updating Deployment: {previous.DeploymentChecksum} -> {next.DeploymentChecksum}");
    }

    public override async Task DeleteDeployment(FinalizerProperties current) {
        LogInfo($"Deleting Deployment: {current.DeploymentChecksum}");

        // enumerate all S3 objects
        var request = new ListObjectsV2Request {
            BucketName = _bucketName
        };
        var counter = 0;
        var deletions = new List<Task>();
        do {
            var response = await _s3Client.ListObjectsV2Async(request);

            // delete any objects found
            if(response.S3Objects.Any()) {
                deletions.Add(_s3Client.DeleteObjectsAsync(new DeleteObjectsRequest {
                    BucketName = _bucketName,
                    Objects = response.S3Objects.Select(s3 => new KeyVersion {
                        Key = s3.Key
                    }).ToList(),
                    Quiet = true
                }));
                counter += response.S3Objects.Count;
            }

            // continue until no more objects can be fetched
            request.ContinuationToken = response.NextContinuationToken;
        } while(request.ContinuationToken != null);

        // wait for all deletions to complete
        await Task.WhenAll(deletions);
        LogInfo($"Deleted {counter:N0} objects");
    }
}
```
