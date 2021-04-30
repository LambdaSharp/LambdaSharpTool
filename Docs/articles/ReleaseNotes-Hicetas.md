﻿---
title: LambdaSharp "Hicetas" Release (v0.8)
description: Release notes for LambdaSharp "Hicetas" (v0.8)
keywords: release, notes, hicetas
---

# LambdaSharp "Hicetas" Release (v0.8.2.4) - 2021-04-30

> Hicetas was a Greek philosopher of the Pythagorean School. He was born in Syracuse. Like his fellow Pythagorean Ecphantus and the Academic Heraclides Ponticus, he believed that the daily movement of permanent stars was caused by the rotation of the Earth around its axis. When Copernicus referred to Nicetus Syracusanus (Nicetus of Syracuse) in _De revolutionibus orbium coelestium_ as having been cited by Cicero as an ancient who also argued that the Earth moved, it is believed that he was actually referring to Hicetas. [(Wikipedia)](https://en.wikipedia.org/wiki/Hicetas)


## What's New

This release introduces some key new capabilities for Lambda functions and the _LambdaSharp.Core_ module. The [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) base class has new methods for [sending events](xref:LambdaSharp.ALambdaFunction.LogEvent``1(System.String,System.String,``0,System.Collections.Generic.IEnumerable{System.String})), [capturing metrics](xref:LambdaSharp.ALambdaFunction.LogMetric(System.Collections.Generic.IEnumerable{LambdaSharp.Logging.Metrics.LambdaMetric})), and [logging debug statements](xref:LambdaSharp.ALambdaFunction.LogDebug(System.String,System.Object[])). In addition, the _LambdaSharp.Core_ module now uses [Amazon Kinesis Firehose](https://aws.amazon.com/kinesis/data-firehose/) for ingesting CloudWatch Log streams. Kinesis Firehose is more cost effective, scales automatically, and also provides built-in integration with S3 for retaining ingested logs. Finally, during the ingestion process, the logs are converted into [queryable JSON documents](LogRecords.md).

### Upgrading from v0.7 to v0.8
1. Ensure all modules are deployed with v0.6.1 or later
1. Upgrade LambdaSharp CLI to v0.8
    1. `dotnet tool update -g LambdaSharp.Tool`
1. Upgrade LambdaSharp Deployment Tier (replace `Sandbox` with the name of the deployment tier to upgrade)
    1. `lash init --allow-upgrade --tier Sandbox`


## BREAKING CHANGES

### LambdaSharp Core Services

#### LambdaSharp.Core

Some configuration parameters changed with the switch to [Amazon Kinesis Firehose](https://aws.amazon.com/kinesis/data-firehose/) for CloudWatch logs ingestion.
* The `LoggingStreamRetentionPeriodHours` and `LoggingStreamShardCount` are no longer needed and have been removed.
* The `LoggingStream` has been replaced by `LoggingFirehoseStream`.
* The `LoggingBucketSuccessPrefix` and `LoggingBucketFailurePrefix` have been added.

See the [LambdaSharp.Core](~/modules/LambdaSharp-Core.md) module documentation for more details.


### LambdaSharp SDK

**IMPORTANT:** In preparation to switching `LambdaSharp` assembly to .NET Core 3.1 and using [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to) as JSON serializer in a future release, it is highly advised to remove all references to `Amazon.Lambda.Serialization.Json` and `Newtonsoft.Json` assemblies from all _.csproj_ files. These package are instead inherited via the `LambdaSharp` assembly. These changes ensure that once `LambdaSharp` assembly switches its serialization implementation, dependent projects will cease to compile, making it easier to transition to the new JSON serializer. Otherwise, these projects would compile successfully, but fail at runtime, as it is not possible to mix `Newtonsoft.Json` and `System.Text.Json` serializations.

Look for the following package references in your _.csproj_ files and remove them.
```xml
<PackageReference Include="Amazon.Lambda.Core" Version="1.1.0" />
<PackageReference Include="Amazon.Lambda.Serialization.Json" Version="1.5.0" />
<PackageReference Include="Newtonsoft.Json" Version="12.0.3" />
```

In addition, Lambda functions no longer need to declare the default serializer with the `[assembly: LambdaSerializer(...)]` attribute, unless a custom serializer is required. Instead, the serializer is defined by the base class ([ALambdaFunction](xref:LambdaSharp.ALambdaFunction)). This change was made to ease the transition to the new [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to) serializer in a future release.

Finally, the `SerializeJson(...)` and `DeserializeJson<T>(...)` have been marked as obsolete. Instead, use `LambdaSerializer.Serialize(...)` and `LambdaSerialize.Deserialize<T>(...)`, respectively.


## New LambdaSharp Module Syntax

### CloudWatch EventBridge Source

Lambda functions can now subscribe to a CloudWatch event bus. The [EventBus notation](~/syntax/Module-Function-Sources-EventBus.md) is straightforward. Multiple event bus subscriptions can be active at the same time.

```yaml
Sources:
  - EventBus: default
    Pattern:
      source:
        - Sample.Event
      detail-type:
        - MyEvent
      resources:
        - !Sub "lambdasharp:tier:${Deployment::Tier}"
```

### Package Build Command

The `Package` declaration now supports and optional [`Build` attribute](~/syntax/Module-Package.md), which specifies a command to run before the zip package is created. This enables creating packages from files that are generated by another project. For example, the output folder when publishing a [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) application.

```yaml
- Package: MyBlazorPackage
  Build: dotnet publish -c Release MyBlazorApp
  Files: MyBlazorApp/bin/Release/netstandard2.1/publish/wwwroot/
```

### New Module Variables

New module variables have been introduced that relate to the deployment. These include `Deployment::BucketName`, `Deployment::Tier`, and `Deployment::TierLowercase`. For more details, see the [Module Global Variables](~/syntax/Module-Global-Variables.md) documentation.


## New LambdaSharp CLI Features

The CLI remains mostly unchanged from the previous release.

### Misc.
* Updated embedded CloudFormation spec to 14.3.0.
* Enhanced API Gateway V2 WebSocket logging to show error messages.
* Enabled detailed CloudWatch metrics for API Gateway deployments.
* Enhanced `lash init` to highlight deployment tier name during stack updates.
* Enhanced `lash init` for _LambdaSharp_ contributors to automatically force build and force publish.
* Enhanced `lash nuke` to only empty the deployment and logging buckets if they were created by the _LambdaSharp.Core_ module.
* Added `--no-ansi` option to `util delete-orphan-logs`, `util download-cloudformation-spec`, and `util create-invoke-methods-schema`.
* Added `util validate-assembly` command.

### Fixes
* Fixed an issue were recreating a _LambdaSharp.Core_ deployment from scratch would not update existing deployed modules with the new deployment bucket name.
* Let CloudFormation determine the name for `AWS::ApiGateway::Model` resources.
* Fixed an issue where the `--aws-region` command option didn't always work as expected.


## New LambdaSharp SDK Features

### CloudWatch Metrics

Several Lambda function base classes have been enhanced with [custom CloudWatch metrics](Metrics-Function.md) to augment existing AWS metrics. In addition, the [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) base class has new [LogMetric(...)](xref:LambdaSharp.ALambdaFunction.LogMetric(System.Collections.Generic.IEnumerable{LambdaSharp.Logging.Metrics.LambdaMetric})) methods to emit custom CloudWatch metrics using the [embedded metric format](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html).

[The MetricSample module](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/MetricSample) shows how to use the new [LogMetric(...)](xref:LambdaSharp.ALambdaFunction.LogMetric(System.String,System.Double,LambdaSharp.Logging.Metrics.LambdaMetricUnit)) methods to emit custom CloudWatch metrics using the [embedded metric format](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html).

### CloudWatch Events

The [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) base class has a new [LogEvent(...)](xref:LambdaSharp.ALambdaFunction.LogEvent``1(System.String,System.String,``0,System.Collections.Generic.IEnumerable{System.String})) method to emit CloudWatch Events to the default event bus on [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/what-is-amazon-eventbridge.html).

[The EventSample module](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/EventSample) shows how to use the new [LogEvent(...)](xref:LambdaSharp.ALambdaFunction.LogEvent``1(System.String,System.String,``0,System.Collections.Generic.IEnumerable{System.String})) method to emit CloudWatch Events to the default event bus on [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/what-is-amazon-eventbridge.html) method for sending CloudWatch Events.

### Debug Logging

The [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) has a new [LogDebug(string format, params object[] arguments)](xref:LambdaSharp.ALambdaFunction.LogDebug(System.String,System.Object[])) method to make debug logging easier. In addition, when debug logging is enabled, the base class logs the request and response streams to CloudWatch logs. See the documentation on [Lambda Debugging](Debugging.md) for more details.


## New LambdaSharp Core Services Features

The _LambdaSharp.Core_ module now uses [Amazon Kinesis Firehose](https://aws.amazon.com/kinesis/data-firehose/) for ingesting CloudWatch Log streams. During the ingestion process, the logs are converted into [queryable JSON documents](LogRecords.md).

In addition, _LambdaSharp.Core_ now emits [operational CloudWatch events](Events.md) to the default event bus, which can be used to observe and track deployed modules.

Finally, the _LambdaSharp.Core_ module emits its [own performance metrics](Metrics-Module.md) to provide quick insight into its operations.

### Misc.

Part of this release, _LambdaSharp.Core_ functions were ported to .NET Core 3.1 with null-aware code, and the modules were published with _ReadyToRun_ for shorter cold-start times.

### Fixes
* Fixed an issue with processing the Lambda report lines in the CloudWatch logs.


## Releases

### (v0.8.2.4) - 2021-04-30

### Features

* Modules
  * _LambdaSharp.S3.Subscriber_
    * Added `ResourceHandlerRole` as export value from module to allow dependent stacks to import it and add policies to it when needed.

#### Fixes

* CLI
  * Fixed a regression that caused the _Beep_ sound not to be played anymore after a long operation.

* SDK
  * Fixed a possible `NullReferenceException` when module info is not set for a Lambda function.

* Modules
  * _LambdaSharp.S3.Subscriber_
    * Fixed an issue where buckets in another region or inaccessible would cause an _internal error_ rather than respond with an informative message.

* Samples
  * Enabled debug output for _Sample.Debug_ module.

### (v0.8.2.3) - 2021-04-01

#### Features

* CLI
  * Added `--from-bucket` as alias for `--from-origin` to make the intent clearer (`--from-origin` is still supported for backwards compatibility).

* Syntax
  * Added `stack` as a scope keyword to make an item available from a nested stack, but not publicly available for import.
  * Added `Module::RestApi::CorsOrigin` as a referenceable variable to the module environment.

#### Fixes

* CLI
  * Fixed an issue where cached module versions were not refreshed when importing a new module to a bucket.
  * Fixed an issue where `--existing-s3-bucket-name` was not respected when `--quick-start` was used.
  * Fixed an issue where performance statistics were no longer shown for externally invoked tools.
  * Fixed an issue where the `$default` WebSocket route could not respond with a custom payload.

* Modules
  * _LambdaSharp.App.Api_
    * Use `stack` scope outputs variables instead of `public` scope to prevent them being accidentally imported.
  * _LambdaSharp.App.Bucket_
    * Use `stack` scope outputs variables instead of `public` scope to prevent them being accidentally imported.
  * _LambdaSharp.App.EventBus_
    * Enforce stricter adherence to protocol with client.
    * Respond to unknown actions with an error instead of silently ignoring them.
    * Use `stack` scope outputs variables instead of `public` scope to prevent them being accidentally imported.
    * Added `WebSocketApiId` as output value so that the WebSocket API can be referenced from the parent stack.

### (v0.8.2.2) - 2021-03-17

#### BREAKING CHANGES

* SDK
    * Renamed _LambdaSharp.CloudWatch_ assembly to _LambdaSharp.EventBridge_ to align with new AWS branding for CloudWatch Events.

#### Features

* CLI
  * Added automatic, daily refresh of CloudFormation specification to surface latest capabilities. Use `--force-refresh` option to check for new CloudFormation specification more frequently.
  * Added caching of AWS profile information used for building, publishing, and deploying modules. Use `--force-refresh` option to bypass cache.
  * Removed `util download-cloudformation-spec` command since it is no longer needed to CloudFormation specification.
  * Changed behavior for `deploy` to allow importing dependencies when no build policy is provided.

### (v0.8.2.1) - 2021-02-17

#### Fixes

* CLI
  * Fixed an issue with _ReadyToRun_ Lambda function compilation.

### (v0.8.2.0) - 2021-02-15

#### BREAKING CHANGES

* SDK
  * LambdaSharp base classes for Lambda functions now require the JSON serializer to be provided. The JSON serializer must derive from `LambdaSharp.Serialization.ILambdaJsonSerializer`. The preferred JSON serializer is `LambdaSystemTextJsonSerializer`.
  * Renamed `LambdaJsonSerializer` to `LambdaNewtonsoftJsonSerializer` to make it clear this JSON serializer is based on `Newtonsoft.Json`.
  * Moved `LambdaNewtonsoftJsonSerializer` to its own NuGet package _LambdaSharp.Serialization.NewtonsoftJson_ to remove dependency on the _Newtonsoft.Json_ NuGet package for _LambdaSharp_ assembly.
  * Changed `ALambdaFunction.LambdaSerializer` property type to `ILambdaJsonSerializer`.
  * Changed target framework for `LambdaSharp` project to `netcoreapp3.1` which is required by the `Amazon.Lambda.Serialization.SystemTextJson` assembly.
  * Changed target framework for `LambdaSharp.Slack` project to `netcoreapp3.1` which is required by the `LambdaSharp` assembly.
  * Updated _AWSSDK.Core_ dependencies to version 3.5
  * Changed method signatures in `ALambdaCustomResourceFunction`:
    * Added `System.Threading.CancellationToken` parameter to `ProcessCreateResourceAsync`, `ProcessUpdateResourceAsync`, and `ProcessDeleteResourceAsync`.
  * Changed method signatures in `ALambdaFinalizerFunction`:
    * Renamed `CreateDeployment` to `CreateDeploymentAsync`, `CreateDeployment` to `CreateDeploymentAsync`, and `DeleteDeployment` to `DeleteDeploymentAsync`.
    * Added `System.Threading.CancellationToken` parameter to `CreateDeploymentAsync`, `CreateDeploymentAsync`, and `DeleteDeploymentAsync`.
  * Moved the LambdaSharp namespaces to specialized assemblies:
    * _LambdaSharp.ApiGateway_
    * _LambdaSharp.CloudWatch_
    * _LambdaSharp.CustomResource_
    * _LambdaSharp.Finalizer_
    * _LambdaSharp.Schedule_
    * _LambdaSharp.SimpleNotificationService_
    * _LambdaSharp.SimpleQueueService_

##### NuGet Package Migration Guide

|Base Class                                             |NuGet Package                         |
|-------------------------------------------------------|--------------------------------------|
|ALambdaApiGatewayFunction<TRequest,TResponse>          |LambdaSharp.ApiGateway
|ALambdaCustomResourceFunction<TProperties,TAttributes> |LambdaSharp.CustomResource
|ALambdaEventFunction<TMessage>                         |LambdaSharp.CloudWatch
|ALambdaFinalizerFunction                               |LambdaSharp.Finalizer
|ALambdaFunction                                        |LambdaSharp
|ALambdaFunction<TRequest,TResponse>                    |LambdaSharp
|ALambdaQueueFunction<TMessage>                         |LambdaSharp.SimpleQueueService
|ALambdaScheduleFunction                                |LambdaSharp.Schedule
|ALambdaTopicFunction<TMessage>                         |LambdaSharp.SimpleNotificationService

#### Features

* CLI
  * Added support for self-contained .NET 5 Lambda functions.
  * Updated Blazor WebAssembly app template to target .NET 5.
  * Removed dependency on _Amazon.Lambda.Tools_. _Amazon.Lambda.Tools_ is no longer requires to build, publish, or deploy LambdaSharp modules.
  * Updated CloudFormation to v22.0.0.
  * Allowed resource types to have an empty or omitted `Attributes` section.

* SDK
  * Added new `ALambdaEventFunction<TMessage>` base class for handling CloudWatch events.
  * Optimized cold-start times for by deserializing Amazon Lambda data-structures by using `LambdaSystemTextJsonSerializer`, which is built on _System.Text.Json_. This change avoids jitting the heavy _Newtonsoft.Json_ assembly unless required by the end-user code.
  * Added constructor to customize serializer settings for `LambdaSystemTextJsonSerializer`.
  * Added constructor to customize serializer settings for `LambdaNewtonsoftJsonSerializer`.
  * Added null-aware annotations to _LambdaSharp_ assembly.
  * Added [MD5 algorithm implementation from Mono](https://github.com/mono/mono/blob/master/mcs/class/corlib/System.Security.Cryptography/MD5CryptoServiceProvider.cs) in _LambdaSharp.Logging_ since it is not supported in .NET 5 WebAssembly currently.
  * Added logic in `ALambdaCustomResourceFunction` and `ALambdaFinalizerFunction` to trigger the `System.Threading.CancellationToken` 500ms before the function times out.
  * Added `TerminateLambdaInstance()` method, which forces the Lambda instance to terminate and perform a cold start on next invocation. This method should only be used when the processing environment has become corrupted beyond repair.

* Syntax
  * Added `Module::RestApi::CorsOrigin` override to specify an expression as the _CORS Origin_ header value. The _CORS Origin_ header value is automatically added to all REST API responses.
  * Added `Stack` as the declaration keyword for nested stacks. Previously the keyword was `Nested`, which remains supported for backwards compatibility.

* Samples
  * Added `Samples/JsonSerializerSample` module showing how to specify and customize the JSON serializer for a Lambda function.
  * Added `Samples/LambdaSelfContainedSample` module showing how to build a Lambda function using .NET 5.

### (v0.8.1.6) - 2020-12-28

#### Features

* CLI
  * Updated embedded CloudFormation spec to 22.0.0.

#### Fixes

* Modules
  * Fixed a critical issue in _LambdaSharp.Core_ where the CloudWatch Logs analyzer would get overwhelmed at times. This would cause the same batch of log events to be processed repeatedly causing a storm of CloudWatch event notifications until the batch finally failed. Failed batches can be found in the logging bucket under the `logging-failed/` prefix.

### (v0.8.1.5) - 2020-12-26

#### Features

* CLI
  * Enhanced `lash new` command to prompt for type of file to create (app, function, or resource). Defaults to `module` when no `Module.yml` file is present.
  * Enhanced `lash util list-modules` command to show a message when pre-release modules were omitted from the list.

#### Fixes

* CLI
  * Fixed a NRE in `lash util list-modules` when the bucket name was passed in as an option instead of an argument.

### (v0.8.1.4) - 2020-11-20

#### Fixes

* CLI
  * Fixed an issue where the `nuke` command did not delete all stacks as expected.
  * Fixed an issue where interactive commands would cause an audible alert for short running operations.

### (v0.8.1.3) - 2020-10-21

#### Fixes

* SDK
  * Fixes a broken package reference in the `LambdaSharp.App` package.

### (v0.8.1.2) - 2020-10-20

#### BREAKING CHANGES

* CLI
  * Removed support for Scala-based Lambda functions.

#### Features

* CLI
  * Enhanced the `EventBus` pattern parsing to insert a default LambdaSharp tier constraint for `resources` when not specified. Use `resources: null` to omit the default constraint.
  * Enhanced the `EventBus` pattern parsing to convert `Source` to `source`, `DetailType` to `detail-type`, `Detail` to `detail`, and so on for better consistency with the SDK. Note the renaming is only applied to top-level properties.
  * Added audible cue when `lash` command finishes after running for more than 10s. The audible cue can be suppressed with `--no-beep` option.
  * Enhanced `lash init --quick-start` to work with the `--core-services enabled` option to allow creating a deployment tier without interactive prompts.
  * Added support for recognizing `APIGatewayHttpApiV2ProxyRequest` request parameter in API Gateway proxy functions.

* Modules
  * _LambdaSharp.App.Api_
    * Added `EventSource` parameter to override the CloudWatch event source specified by the app request.
    * Enhanced CloudWatch event integration by explicitly populating the `Resources` section of the app request.
  * _LambdaSharp.App.EventBus_
    * Added module for subscribing apps to CloudWatch events.

* SDK
  * Enhanced emitted CloudWatch events to include app identifier in resources section.
  * Added `ForceLambdaColdStart()` method in `ALambdaFunction` base class. This method forces the Lambda runtime to error out and restart the app domain, forcing the global environment to be reinitialized.
  * Added `LogEvent<T>(T, IEnumerable<string>)` that omits the source name by default to `${Module::FullName}::${AppOrFunctionName}`
  * Added `LambdaSharpEventBusClient` for Blazor apps to connect to the LambdaSharp App EventBus.

#### Fixes

* CLI
  * Fixed an issue with `--build-policy` option affecting the _publish_ and _deploy_ phases.
  * Fixed an issue with detecting `APIGatewayHttpApiV2ProxyResponse` as an API Gateway proxy request data type.
  * Fixed an issue with environment variables set in the Lambda function properties section being overwritten by the tool.
  * Fixed an issue with built-in WebSocket routes being reported as asynchronous API Gateway invocations when they are not.

* SDK
  * Fixed an issue where `Logging:LogLevel` was not properly recognized for Blazor apps in `appsettings.json` file.

### (v0.8.1.1) - 2020-08-31

#### Features

* CLI
  * Added `util list-modules` command to list published modules and versions.
  * Added `--build-policy` option for `build`, `publish`, and `deploy` commands to control which module dependencies are allowed during the build phase.

### (v0.8.1.0) - 2020-08-28

This release introduce support for Blazor WebAssembly apps at the syntax and SDK level. Similar to Lambda functions, Blazor apps have access to the `ILambdaSharpLogger` to log messages, metrics, and events to CloudWatch. This capability is accomplished by generating a REST API that enables the front-end Blazor app to securely integrate with CloudWatch Logs, Metrics, and EventBridge. By default, Blazor apps are served from an S3 bucket configured as a website. Optionally, the S3 bucket can be configured for integration with CloudFront.

#### BREAKING CHANGES

* SDK
  * Moved logging related functionality from _LambdaSharp_ assembly to _LambdaSharp.Logging_.
  * Renamed `ILambdaLogLevelLogger` to `ILambdaSharpLogger`.
  * Renamed `ALambdaRecord` to `ALambdaLogRecord`.
  * Base class is now based on .NET Standard 2.0 for broader compatibility.

#### Features

* CLI
  * Added `new app` command to add a Blazor WebAssembly app to a module.
  * Added `util extract-assembly-metadata` command to extract version metadata from a Blazor app assembly.
  * Enhanced `util create-invoke-methods-schema` to support _System.Text.Json_ attributes on `[FromBody]` and `[FromUri]` parameters.
  * Updated embedded CloudFormation spec to 18.0.0.

* Modules
  * Added `LambdaSharp::Registration::App` resource type to register deployed apps.
  * Added _LambdaSharp.App.Site_ module for hosting app packages.
  * Added _Lambdasharp.App.Api_ module for creating a CloudWatch Logs and Events proxy API.
  * Enhanced `RollbarProjectPattern` parameter in _LambdaSharp.Core_ to allow `{ModuleId}` and `{ModuleIdNoTierPrefix}` as pattern variables for Rollbar project names.
  * Added support in _LambdaSharp.Core_ for categorizing Lambda exit errors with `Runtime.ExitError` as fatal.

* SDK
  * Enhanced error report platform description to include .NET Core framework description (e.g. `"AWS Lambda [.NET Core 3.1.1] (Unix 4.14.177.104)"`)
  * Added `ALambdaComponent` class for use with Blazor WebAssembly components.

#### Fixes

* CLI
  * Fixed an issue in `util show-kinesis-failed-logs` where logs with multiple entries were not parsed properly.
  * Fixed an issue where a _Finalizer_ would not run when only stack parameters are changed.
  * Fixed an issue with resource timings being updated twice.
  * Fixed an issue where the single-quote character (') was incorrectly escaped by some API Gateway integrations.

### (v0.8.0.9) - 2020-08-31

#### Features

* CLI
  * Added `util list-modules` command to list published modules and versions.
  * Added `--build-policy` option for `build`, `publish`, and `deploy` commands to control which module dependencies are allowed during the build phase.

#### Fixes

* CLI
  * Fixed an issue in `util show-kinesis-failed-logs` where logs with multiple entries were not parsed properly.
  * Fixed an issue where a _Finalizer_ would not run when only stack parameters are changed.
  * Fixed an issue with resource timings being updated twice.
  * Fixed an issue where the single-quote character (') was incorrectly escaped by some API Gateway integrations.

### (v0.8.0.8) - 2020-08-03

#### Fixes

* CLI
  * Fixed a regression in `lash init --quick-start` that failed to create the initial S3 bucket.

### (v0.8.0.7) - 2020-07-28

#### BREAKING CHANGES

* CLI
  * Renamed `new bucket` command to `new public-bucket` to make it clear the created S3 bucket is publicly accessible.

#### Features

* CLI
  * Added `new expiring-bucket` command to create a self-deleting S3 bucket.
  * Enhanced `publish` and `deploy` commands to allow omitting the version number to automatically use the latest published module version.
  * Enhanced `publish` and `deploy` command by adding `--from-origin` option to import modules from specified origin instead the module origin. Dependencies must be published explicitly.
  * Enhanced `deploy` command by adding `--no-import` option to prevent module artifacts or dependencies from being imported. All artifacts must already exist in the deployment tier bucket.
  * Enhanced `deploy` to automatically upgrade shared dependencies when deploying a module. This behavior can be turned off with the `--no-dependency-upgrades` option.
  * Enhanced output for `tier version` command. The command now also returns status code 2 when the deployment tier does not exist.

#### Fixes

* CLI
  * Fixed an issue in `util show-kinesis-failed-logs` where logs with multiple entries were not parsed properly.
  * Fixed an issue in `init` where `--existing-s3-bucket-name` would not convert a S3 bucket name to a S3 bucket ARN.
  * Fixed an issue in `tier coreservices --enabled` would fail without an explanation when the deployment tier was created without _Core Services_.
  * Use `AWS::Partition` instead of hard-coding `aws` when constructing ARNs.
  * Fixed an issue where the _Finalizer_ was not taking a dependency on conditional custom resources.

### (v0.8.0.6) - 2020-07-14

#### Features

* CLI
  * Enabled detailed CloudWatch metrics for WebSocket deployments.
  * Added `lambdasharp:moduleinfo:$MODULE_INFO` and `lambdasharp:origin:$MODULE_ORIGIN` metadata to emitted CloudWatch events.
  * Added support for `!Ref` function in parameter files to read built-in variables.
  * Added `Deployment::BucketName`, `Deployment::Tier`, `Deployment::TierLowercase`, `Deployment::TierPrefix`, `Deployment::TierPrefixLowercase` as built-in variables for parameter files.
  * Updated embedded CloudFormation spec to 16.1.0.

* Syntax
  * Added pragma for overriding `Module::WebSocket.ApiKeySelectionExpression`.
  * Added `Deployment::TierPrefix` and `Deployment::TierPrefixLowercase` as module variables.

#### Fixes

* CLI
  * Fixed an issue where references to missing environment variables in the _parameters.yml_ file were not reported as errors.
  * Fixed an issue where an API Gateway deployment (REST API and WebSocket) was not updated properly when an authorizer was changed.

### (v0.8.0.5) - 2020-07-02

#### Features

* CLI
  * Enhanced validation in `new` command for function and resource names.

#### Fixes

* CLI
  * Fixed an output alignment issue with retained resources that show the `DELETE_SKIPPED` state.
  * Fixed an issue where the timing of resource creation/update operations was miscalculated.
  * Fixed an issue where invalid resource names were not properly reported.

* SDK
  * Fixed an issue where the `DEBUG_LOGGING_ENABLED` value was case-sensitive, instead of case-insensitive.

### (v0.8.0.4) - 2020-07-01

#### Features

* CLI
  * Enhanced `info` command to show the name of the logging S3 bucket for the deployment tier.
  * Enhanced CloudFormation parameter prompts by showing min/max value and min/max length constraints.
  * Enhanced CloudFormation resource creation/update tracking to show how long the operation took.
  * Enhanced `nuke` command to delete retained S3 buckets from _LambdaSharp.Core_.
  * Added `util show-kinesis-failed-logs` command to list CloudWatch log entries that failed to be processed by _LambdaSharp.Core_.
  * Added check for `!If [ condition, ifTrue, ifFalse ]` on publicly scoped variables to make CloudFormation output conditional on `condition`.
  * Added `tier list` command to list all available deployment tiers.
  * Added confirmation prompt when detecting potential replacement/deletion of resources during `lash deploy` instead of erroring.
  * Added `util show-parameters` command to show the processed parameters YAML file.
  * Added support for `!Sub` function in parameter files.
  * Updated embedded CloudFormation spec to 16.0.0.

* Syntax
  * Added pragmas for overriding `Module::RestApi::StageName` and `Module::WebSocket::StageName`.
  * Added support for `DeletionPolicy` attribute on resource and parameter declarations.

* Modules
  * Migrated _LambdaSharp.Twitter.Query_ function implementation to null-aware C#.
  * Added `DeletionPolicy: Retain` to the _LambdaSharp.Core_ S3 logging bucket.

* Misc.
  * Migrated all Lambda function implementations to sealed classes since they are never inherited from.

#### Fixes

* CLI
  * Fixed an issue when processing parameter files where the encryption key in `!GetParam` required the `"alias/"` prefix.

* Modules
  * Increased memory limit for _LambdaSharp.S3.Subscriber_ resource handler to 256MB.

### (v0.8.0.3) - 2020-06-19

#### Features

* CLI
  * Added a check for asynchronous API Gateway method invocations to report when they are used with HTTP GET/OPTIONS.
  * Added `lash tier version` command to show or check the deployment tier version against a minimum expected version.
  * Updated embedded CloudFormation spec to 15.2.0.

* SDK
  * Added publishing of debugging symbols information for _LambdaSharp_ nuget package.

#### Fixes

* CLI
  * Fixed an issue where a function was not recompiled when only its API mappings were modified, which led the function to have an out-of-date mappings file.
  * Fixed an issue where an invalid function schema was saved after the build.

* Modules
  * Fixed an issue in _LambdaSharp.Core_ that caused function registrations not to be updated.
  * Fixed an issue in _LambdaSharp.Core_ that caused near out-of-memory Lambda usage reports to trigger an an out-of-memory error notification instead of a warning.

* Samples
  * Fixed _VpcFunctionSample_ to use `CommaDelimitedList` instead of `CommaDelimitedString`. [Issue #147](https://github.com/LambdaSharp/LambdaSharpTool/issues/147)

### (v0.8.0.2) - 2020-06-02

#### Features

* CLI
  * Added `--skip-apigateway-check` to `lash init` to bypass API Gateway role creation/update operation during deployment tier initialization.

* SDK
  * Added the virtual `DebugLoggingEnabled` property to `ALambdaFunction` as the preferred way to check if debug logging is enabled.

* Syntax
  * Added pragma for overriding `Module::Role.PermissionsBoundary` (contributed by @yurigorokhov).

* Modules
  * Added `Encoding` property for `LambdaSharp::S3::IO` to enable compression encoding for content before it is deployed to an S3 bucket.
  * Added `Content-MD5` checksum header for S3 uploads performed by `LambdaSharp::S3::Unzip` to ensure end-to-end data integrity.

* Samples
  * Added _DebugSample_ showing how to use `LogDebug()` and `DebugLoggingEnabled`.

#### Fixes

* CLI
    * Added support to all commands for `--no-ansi`, `--quiet`, and `--verbose` options.

* Modules
  * Removed unnecessary S3 access policy from _LambdaSharp.Core_ that granted read access to `serverlessrepo.amazonaws.com` for deployment buckets.

### (v0.8.0.1) - 2020-05-18

#### Fixes

* CLI
    * Added fixes from v0.7.0.17 release.
