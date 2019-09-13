![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp Lambda Function with Lambda Layer

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Use a `Package` definition to compress your local files and have them published by the LambdaSharp CLI. Then create a `AWS::Lambda::LayerVersion` referencing the published zip package. Finally, use the `Properties` section on the Lambda function to attach the Lambda layer to the function.

```yaml
Module: Sample.LambdaLayer
Description: A sample module defining a Lambda function with a Lambda Layer
Items:

  - Package: MyLayerFiles
    Description: Zip package of files to include in Lambda Layer
    Files: layer-files/

  - Resource: MyLambdaLayer
    Description: Custom Lambda layer with files from MyLayerFiles package
    Type: AWS::Lambda::LayerVersion
    Properties:
      Content:
        S3Bucket: !Ref DeploymentBucketName
        S3Key: !Ref MyLayerFiles

  - Function: MyFunction
    Description: Lambda function using the custom Lambda layer
    Memory: 128
    Timeout: 30
    Properties:
      Layers:
        - !Ref MyLambdaLayer
```
