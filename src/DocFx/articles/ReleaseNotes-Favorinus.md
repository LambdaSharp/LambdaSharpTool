# λ# - Favorinus (v0.6.0.3) - 2019-07-01

> Favorinus had extensive knowledge, combined with great oratorical powers, that raised him to eminence both in Athens and in Rome. He lived on close terms with Plutarch, with Herodes Atticus, to whom he bequeathed his library in Rome, with Demetrius the Cynic, Cornelius Fronto, Aulus Gellius, and with the emperor Hadrian. [(Wikipedia)](https://en.wikipedia.org/wiki/Favorinus)

## What's New

This release focuses on API Gateway, both for REST APIs and WebSockets. After building the Lambda functions, λ# analyzes the compiled assemblies to enhance the API Gateway rout definitions in CloudFormation. For example, λ# will extract the JSON schema from the target methods to create request validation rules for API Gateway. This enables API Gateway to block requests before they reach the Lambda function. In addition, λ# allows checks if the target method returns a response. If not, API Gateway will be configured to invoke the Lambda function asynchronously, providing much faster response times.

In addition, there is a [new documentation site](https://lambdasharp.net) that covers all aspects of λ#, including the base classes, modules, and syntax.


## BREAKING CHANGES

The following change may impact modules created with previous releases.

### λ# CLI

* The `git-sha` is now prefixed with `DIRTY-` if the git checkout contains modified or untracked files.

### λ# Assemblies

The λ# dependencies were updated to `Amazon.Lambda.Core v1.1.0` and `Amazon.Lambda.Serialization.Json v1.5.0`. All projects need their dependencies updated to reflect this change. Otherwise, a compilation error will occur.

The `LambdaSharp.dll` assembly was refactored for consistency. Classes are now organized into the following namespaces:
* `LambdaSharp`: Common base classes, such as [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) and [LambdaConfig](xref:LambdaSharp.LambdaConfig).
* `LambdaSharp.ApiGateway`: Classes relating to [ALambdaApiGatewayFunction](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction).
* `LambdaSharp.ConfigSource`: Classes for reading configuration information. Usually not required.
* `LambdaSharp.CustomResource`: Classes relating to [ALambdaCustomResourceFunction<TProperties,TAttributes>](xref:LambdaSharp.CustomResource.ALambdaCustomResourceFunction`2).
* `LambdaSharp.ErrorReports`: Classes for generating error reports. Usually not required.
* `LambdaSharp.Exceptions`: Classes for throwing exceptions. Usually not required.
* `LambdaSharp.Finalizer`: Classes relating to [ALambdaFinalizerFunction](xref:LambdaSharp.Finalizer.ALambdaFinalizerFunction).
* `LambdaSharp.Logger`: Classes for basic logging. Usually not required.
* `LambdaSharp.Schedule`: Classes relating to [ALambdaScheduleFunction](xref:LambdaSharp.Schedule.ALambdaScheduleFunction).
* `LambdaSharp.SimpleNotificationService`: Classes relating to [ALambdaTopicFunction<TMessage>](xref:LambdaSharp.SimpleNotificationService.ALambdaTopicFunction`1).
* `LambdaSharp.SimpleQueueService`: Classes relating to [ALambdaQueueFunction<TMessage>`](xref:LambdaSharp.SimpleQueueService.ALambdaQueueFunction`1).

Other functional changes include:
* The `RequestId` property in `ALambdaFunction` was replaced by the more generic `CurrentContext` property. Old code needs to be updated to use `CurrentContext.AwsRequestId` instead.
* The `ILambdaContext context` parameter was removed in favor of the `CurrentContext` property.
* Methods starting with `HandleXYZ` were renamed to `ProcessXYZ` for consistency.


## New λ# Module Features

### API Gateway .NET

The `Api` event-source now supports specifying a target invocation method using the `Invoke` attribute. At compile time, λ# CLI verifies that the target method exists in the compiled assembly. In addition, λ# generates the request/response JSON schema models for the method and uses them to configure API Gateway. If the method has either `void` or `Task` as return type, λ# will configure the lambda function as an asynchronous invocation in API Gateway. Note, for this behavior to take effect, the Lambda function must derive from `ALambdaApiGatewayFunction`.

```yaml
- Function: MyFunction
  Memory: 256
  Timeout: 30
  Sources:

    - Api: POST:/items
      Invoke: AddItem
```

The target method can now be implemented in a straightforward way, without requiring deserializing or validation.

```csharp
public AddItemResponse AddItem(AddItemRequest request) {

    // add new item to list
    var item = new Item {
        Id = Guid.NewGuid().ToString("N"),
        Value = request.Value
    };
    _items.Add(item);

    // respond with new item ID
    return new AddItemResponse {
        Id = item.Id
    };
}
```

### WebSockets .NET

A new `WebSocket` event-source is now supported. Similar to the `Api` event-source, `WebSocket` supports specifying a target invocation method using the `Invoke` attribute. At compile time, λ# CLI verifies that the target method exists in the compiled assembly. In addition, λ# generates the request/response JSON schema models for the method and uses them to configure API Gateway v2. If the method has either `void` or `Task` as return type, λ# will configure the lambda function as an asynchronous invocation in API Gateway v2. Otherwise, it will be configured for bidirectional communication. Note, for this behavior to take effect, the Lambda function must derive from `ALambdaApiGatewayFunction`.

```yaml
- Function: MessageFunction
  Memory: 256
  Timeout: 30
  Sources:

    - WebSocket: send
      Invoke: SendMessageAsync
```

The target method can now be implemented in a straightforward way, without requiring deserializing or validation. The method uses the `WebSocketClient` property, which is inherited from the base class. The `WebSocketClient` returns the `IAmazonApiGatewayManagementApi` instance when `ALambdaApiGatewayFunction` was instantiated with a WebSocket URL.

```csharp
public Task SendMessageAsync(Message request) {

    // echo message back
    return WebSocketClient.PostToConnectionAsync(new PostToConnectionRequest {
        ConnectionId = CurrentRequest.RequestContext.ConnectionId,
        Data = SerializeJson(new Message {
            From = request.From,
            Text = request.Text
        }).ToStream()
    });
}
```

### API Gateway CloudWatch Log

The API Gateway CloudWatch log is now created--and therefore, deleted--by CloudFormation.


## New λ# CLI Features

### Build Command

The `--module-version` option was added to allow overriding the version of the module being compiled. This capability is useful in build pipelines where a script needs to set the current version number.

In addition, the captured git-sha is now prepended with `DIRTY-` when there are uncommitted/untracked changes when the module is built.

### Publish Command

The `--force-publish` option was modified to publish all assets even even when the checksums match.


### Deploy Command

All CloudFormation stacks are now tagged with `LambdaSharp:DeployedBy` to capture the identity of who deployed the stack.


### New Command

The `new module`, `new function`, and `new resource` commands will now prompt for the name and type when omitted.

The `new resource` command now also inserts a documentation link with the newly added resource declaration. In addition, if the exact resource name cannot be found, the `new resource` command will show partial matches.

```yaml
- Resource: MyTopic
  Description: TO-DO - update resource description
  Type: AWS::SNS::Topic
  Properties:
    # Documentation: http://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-properties-sns-topic.html
    DisplayName: String
    KmsMasterKeyId: String
    Subscription:
      - Endpoint: String # Required
        Protocol: String # Required
    TopicName: String
```

### Util Command

The `util create-invoke-methods-schema` was added to help generate JSON schema from compiled methods to be used with `ALambdaApiGatewayFunction`.

### List Command

The output of the `list` command was enhanced to show the module name and version.

```bash
lash list --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.6) - List deployed LambdaSharp modules

Found 2 modules for deployment tier 'Default'

NAME                 MODULE                   STATUS             DATE
LambdaSharp-Core     LambdaSharp.Core:0.6     UPDATE_COMPLETE    2019-04-05 10:36:49
LambdaSharp-S3-IO    LambdaSharp.S3.IO:0.6    UPDATE_COMPLETE    2019-04-05 10:37:19

Done (finished: 4/5/2019 3:36:10 PM; duration: 00:00:01.4137682)
```

### Init Command

The `init` command was improved to detect previous versions of a deployment tier and prompt for perform an upgrade operation, similar to the `config` command.


## New λ# Assembly Features

This release adds [`ALambdaQueueFunction<T>`](xref:LambdaSharp.SimpleQueueService.ALambdaQueueFunction`1) as base class for processing messages from SQS queues.

The [`ALambdaApiGatewayFunction`](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction) was also enhanced to provide support for target methods invocations.

## Releases

### (v0.6.0.3) - 2019-07-01

#### New Features

* Added `--decrypt` option to `lash encrypt`, which decrypts the supplied value before encrypting it again. Useful when changing secret keys.
* Enhanced `lash tier coreservices` to also show nested stacks, but ignore them when using either the `--enable` or `--disable` options.

#### Fixes

* Fixed an issue where the IAM policy for using secret keys was not created/updated before the embedded `DecryptionSecret` function was invoked, leading to a CloudFormation failure.

### (v0.6.0.2) - 2019-06-26

#### New Features
* Modules are now compiled with a new `LambdaSharpCoreServices` parameter that controls if a module binds to λ# Core Services.
* Added `lash tier coreservices` command to enable/disable the use of λ# Core Services in deployed modules.
* Added standard module output parameter `LambdaSharpTool` that captures the version of the λ# CLI used to build the module.

#### Fixes
* Fixed an issue where exceptions thrown in pending tasks submitted using `ALambdaFunction.RunTask()` did not cause the Lambda function to fail.
* Fixed an issue where `lash config` failed to properly initialize the API Gateway Account role when it was set to a deleted role.
* Fixed an issue where the physical ID instead of the ARN was used for custom resource handlers when the handler was a Lambda function.
* Mismatched `lambdasharp` assembly version is now reported as a warning instead of an error when running in contributor mode.
* Disabled warnings for potentially invalid references to conditional resources, because of too many false negatives.

### (v0.6.0.1) - 2019-06-11

#### Updated Documentation
* Added documentation for [API Gateway for .NET](~/articles/APIGateway.md).
* Added [Video Tutorials](~/articles/VideoTutorials.md) section.

#### New Features
* X-Ray Tracing now works for API Gateway V1 and Nested Modules.
* Updated CloudFormation spec to 3.3.0.
* Colorized warning and errors in console output.
* Added `ALambdaFunction.RunTask()` and `ALambdaFunction.AddPendingTask(Task)` to queue background operations that must complete before the Lambda invocation finishes.
* Added `HttpClient` with X-Ray tracing instrumentation to `ALambdaFunction` base class.
* `ALambdaFunction` now unrolls `AggregateException` instances and reports errors individually.
* `lash util delete-orphan-logs` replaces `delete-orphan-lambda-logs`, which now also delete orphaned API Gateway V1/V2 CloudWatch logs

#### Fixes
* Fixed an issue with setting the correct IAM role for the API Gateway account.
* Various bug fixes to generating the proper JSON schema for API Gateway endpoints and routes.
* `lash publish` now warns if version already exists instead of reporting an error.
