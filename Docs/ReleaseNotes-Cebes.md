# λ# - Cebes (v0.3) - 2018-09-19

> Cebes was a disciple of Socrates and Philolaus, and a friend of Simmias of Thebes. He is one of the speakers in the Phaedo of Plato, in which he is represented as an earnest seeker after virtue and truth, keen in argument and cautious in decision. Xenophon says he was a member of Socrates' inner circle, and a frequent visitor to the hetaera, Theodote, in Athens.[1] He is also mentioned by Plato in the Crito and Epistle XIII. [(Wikipedia)](https://en.wikipedia.org/wiki/Cebes)

## BREAKING CHANGES

The following change may impact modules you have created using previous releases.

### Bootstrap

The bootstrap procedure has been updated to include a new custom resource to handle S3 bucket subscriptions (see below). The new [Bootstrap](../Bootstrap/) procedure must be run to update the λ# environment.

### Default Module Filename

The default module filename was changed from `Deploy.yml` to `Module.yml` to make it more consistent with the new terminology adopted since the v0.2 release.

A work-around to avoid being impacted by this change is to specify the module filename explicitly.

```bash
lash deploy Deploy.yml
```

### S3 Bucket Notifications as Lambda Source

This release introduces a custom resource to handle subscribing to S3 bucket notifications. In previous releases, it was only possible to subscribe to S3 notifications for S3 buckets that were created in the same λ# module. With the addition of the [`S3Subscriber`](../Bootstrap/LambdaSharpS3Subscriber/) custom resource, it now possible to subscribe to both new and existing S3 buckets. However, this change is not backwards compatible with how previous implementation handled S3 bucket subscriptions and requires the old S3 bucket to be deleted.

### λ# Environment Variable

The name of the deployment tier environment variable was changed to `LAMBDASHARP_TIER`. The `LAMBDASHARP_PROFILE` was added to allow selecting a different, default AWS profile for λ# deployments.

## New λ# Tool Features

### Updated `New` Command

The λ# tool has a new command to create a new module file. This command creates a `Module.yml` file in the current directory.

To invoke the new command:
```bash
lash new module MyNewModule
```

Similarly, the existing `new function` command was updated to match.
```bash
lash new function MyFunction
```

Additionally, the `new function` command now updates the `Module.yml` file by adding a function definition.

### Artifact Output Directory

The λ# tool now generates all artifacts in a dedicated output directory. By default, the output directory is called `bin` and co-located with the input `Module.yml` file.

The location of the output directory can be overwritten with the `--output` option.

```bash
lash deploy --output MyOutputFolder
```

### Validate λ# Assembly References

The λ# tool now checks the version of all λ# assembly references in the function project files. If an assembly version mismatch is found, an error is emitted and deployment operation is cancelled. This behavior can be suppressed with the `--skip-assembly-validation` option.

```bash
lash deploy --skip-assembly-validation
```

To validate assembly references without deploying, use the `--dryrun` option.

```bash
lash deploy --dryrun
```

## New λ# Module Features

### Short-Form CloudFormation Intrinsic Functions

It is now possible to use the YAML short form for [CloudFormation intrinsic functions](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference.html). The short form for the following functions is now supported:
`!And`, `!Base64`, `!Cidr`, `!Equals`, `!FindInMap`, `!GetAtt`, `!GetAZs`, `!If`, `!ImportValue`, `!Join`, `!Not`, `!Or`, `!Ref`, `!Select`, `!Split`, `!Sub`, `!Transform`.

The short form functions can be used for resources, parameters, and Lambda environment variables.

__Resource__

Create an SNS topic that uses the AWS region and CloudFormation stack name as display name.

```yaml
Parameters:
  - Name: MyResource
    Resource:
      Type: AWS::SNS::Topic
      Properties:
        DisplayName: !Sub "${AWS::Region}-${AWS::StackName}"
```

__Parameters__

Capture the CloudFormation stack name as a Lambda parameter.

```yaml
Parameters:
  - Name: MyParameter
    Value: !Ref AWS::StackName
```

__Environment Variables__

Make the AWS region available as a Lambda environment variable.

```yaml
Functions:
  - Name: MyFunction
    Memory: 128
    Timeout: 30
    Environment:
        MyVariable: !Ref AWS::Region
```

### Kinesis Stream as Lambda Source

Lambda functions can now use Kinesis streams as event sources.
See [Kinesis Stream](../Samples/KinesisSample/) sample.

```yaml
Parameters:
  - Name: MyKinesisStream
    Resource:
      Type: AWS::Kinesis::Stream
      Properties:
        ShardCount: 1

Functions:
  - Name: MyFunction
    Memory: 128
    Timeout: 15
    Sources:
      - Kinesis: MyKinesisStream
        BatchSize: 15
```

### DynamoDB Stream as Lambda Source

Lambda functions can now use DynamoDB streams as event sources. See [DynamoDB Stream](../Samples/DynamoDBSample/) sample.

```yaml
Parameters:
  - Name: MyDynamoDBTable
    Resource:
      Type: AWS::DynamoDB::Table
      Allow: Subscribe
      Properties:
        AttributeDefinitions:
          - AttributeName: MessageId
            AttributeType: S
        KeySchema:
          - AttributeName: MessageId
            KeyType: HASH
        ProvisionedThroughput:
          ReadCapacityUnits: 1
          WriteCapacityUnits: 1

Functions:
  - Name: MyFunction
    Memory: 128
    Timeout: 15
    Sources:
      - DynamoDB: MyDynamoDBTable
        BatchSize: 15
```

### Export Lambda Functions

It is now possible to export a Lambda function to the parameter store in a similar way as parameters. This makes it possible for other modules to reference exported Lambda functions.

```yaml
Functions:
  - Name: MyFunction
    Memory: 128
    Timeout: 15
    Export: MyExportedFunction
```

### Collection of Lambda Parameters

Parameters can now have nested collections of parameters. This is useful when a variable number of parameters is required by a Lambda function.

```yaml
Parameters:
  - Name: MyCollection
    Parameters:

      - Name: First
        Value: 1st Parameter

      - Name: Second
        Value: 2nd Parameter
```

The Lambda function can retrieve the names of the nested parameters by using the `Keys` property during initialization.

```csharp
public override Task InitializeAsync(LambdaConfig config) {
    var collection = config["MyCollection"];
    foreach(var key in collection.Keys) {
        _parameters[key] = collection.ReadText(key);
    }
    return Task.CompletedTask;
}
```

### Support CloudFormation Macros (Experimental)

This release includes experimental support for creating [CloudFormation Macros](https://aws.amazon.com/blogs/aws/cloudformation-macros/). See [CloudFormation Macro](../Samples/MacroSample/) sample.

Note that while macros can be defined by a λ# module, they cannot be invoked by a λ# module, because they require CloudFormation stack updates to use change sets, which is not yet supported.


The following function definition creates a new macro called `{{Tier}}-MyMacro` that can be invoked by other CloudFormation change sets.

```yaml
Functions:
  - Name: Function
    Memory: 128
    Timeout: 30
    Sources:
      - Macro: MyMacro
```

## Fixes

* Fixed an issue where some CloudFormation properties needed to suffixed with `_` to work with [Humidifier](https://github.com/jakejscott/Humidifier) library for generating CloudFormation templates correctly.

## Internal Changes

* Improved how Lambda function parameters are passed in. Instead of relying on an embedded `parameters.json` file, parameters are now passed in via environment variables. This means that Lambda function packages no longer need to be re-uploaded because of parameter changes.
* Included `Sid` attribute for all built-in, automatic permissions being added to provide more context.
* Switched to native AWS Lambda `JsonSerializer` class for serializing/deserializing data.
* Upgraded [YamlDotNet](https://github.com/aaubry/YamlDotNet) library to 5.0.1
