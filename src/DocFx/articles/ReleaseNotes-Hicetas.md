---
title: LambdaSharp "Hicetas" Release (v0.8)
description: Release notes for LambdaSharp "Hicetas" (v0.8)
keywords: release, notes, hicetas
---

> TODO: document the JSON format for log records


# LambdaSharp "Hicetas" Release (v0.8.0.0) - TBD

> Hicetas was a Greek philosopher of the Pythagorean School. He was born in Syracuse. Like his fellow Pythagorean Ecphantus and the Academic Heraclides Ponticus, he believed that the daily movement of permanent stars was caused by the rotation of the Earth around its axis. When Copernicus referred to Nicetus Syracusanus (Nicetus of Syracuse) in _De revolutionibus orbium coelestium_ as having been cited by Cicero as an ancient who also argued that the Earth moved, it is believed that he was actually referring to Hicetas. [(Wikipedia)](https://en.wikipedia.org/wiki/Hicetas)


## What's New

This release introduces some key new capabilities for Lambda functions and the _LambdaSharp.Core_ module. The [`ALambdaFunction`](xref:Lambdasharp.ALambdaFunction) base class now has new methods for sending events and recording metrics.

> TODO: more text here

### Upgrading from v0.7 to v0.8
1. Ensure all modules are deployed with v0.6.1 or later
1. Upgrade LambdaSharp CLI to v0.8
    1. `dotnet tool update -g LambdaSharp.Tool`
1. Upgrade LambdaSharp Deployment Tier (replace `Sandbox` with the name of the deployment tier to upgrade)
    1. `lash init --allow-upgrade --tier Sandbox`


## BREAKING CHANGES

### LambdaSharp Core Services

#### LambdaSharp.Core

> TODO:
> * replaced input parameter `LoggingStream` with `LoggingFirehoseStream`
> * removed `LoggingStreamRetentionPeriodHours`
> * removed `LoggingStreamShardCount`
> * added `LoggingBucketSuccessPrefix` and `LoggingBucketFailurePrefix`

### LambdaSharp CLI
* Check for redundant `[assembly: LambdaSerializer(typeof(...))]` definition.

### LambdaSharp SDK

**IMPORTANT:** In preparation of switching `LambdaSharp` assembly to .NET Core 3.1 and using `System.Text.Json` in a future release, it is highly advised to remove all references to `Amazon.Lambda.Serialization.Json` and `Newtonsoft.Json` from all _.csproj_ files. These package are currently inherited via the `LambdaSharp` assembly anyway. This change will ensure that these obsolete references are no longer included once a project is upgraded to using the future release of `LambdaSharp`. Additionally, the default assembly serializer should be switched from `Amazon.Lambda.Serialization.Json.JsonSerializer` to `LambdaSharp.Serialization.LambdaJsonSerializer`.

* Removed package reference to `Amazon.Lambda.Serialization.Json` from all _.csproj_ files as this package is inherited via the `LambdaSharp` reference.
* Removed package reference to `Newtonsoft.Json` from all _.csproj_ files as this package is inherited via the `LambdaSharp` reference.
* Remove `[assembly: LambdaSerializer(...)]` attribute. It's no longer needed as the `LambdaSharp.ALambdaFunction` automatically defines already `[LambdaSerializer(typeof(LambdaSharp.Serialization.LambdaJsonSerializer))]` on the handler entry point.
* Marked `SerializeJson()` and `DeserializeJson()` methods as obsolete. Use `LambdaSerialize.Serialize()` and `LambdaSerializer.Deserialize()` respectively.

## New LambdaSharp Module Syntax

### CloudWatch EventBridge Source

It is now possible to subscribe a Lambda function to a CloudWatch event bus. The [notation](~/syntax/Module-Function-Sources-EventBus.md) is straightforward. Multiple event bus subscriptions can be active at the same time.

```yaml
Sources:
  - EventBus: default
    Pattern:
      source:
        - Sample.Event
      detail-type:
        - MyEvent
      resources:
        - !Sub "lambdasharp:tier:${Module::Tier}"
```

### Package Build Attribute

> TODO:


### New Module Variables

Two new, but related, module variables were introduced to retrieve the deployment tier name. They are `Module::Tier` and `Module::TierLowercase`. As the name implies, `Module::TierLowercase` returns the lowercase version of `Module::Tier`.


## New LambdaSharp CLI Features

* Enhanced `lash init` to highlight deployment tier name during stack updates.
* Enhanced `lash init` for _LambdaSharp_ contributors to automatically force build and force publish.
* Enhanced `lash nuke` to only empty the deployment and logging buckets if they were created by the _LambdaSharp.Core_ module.

## New LambdaSharp SDK Features

> TODO: talk about new emitted metrics and link to docs
> * Added CloudWatch metrics to `ALambdaQueueFunction<T>` base class.
> * Added CloudWatch metrics to `ALambdaTopicFunction<T>` base class.
> * Talk about custom metrics.

* Added [`ALambdaFunction.LogMetric(...)`](xref:ALambdaFunction.LogMetric(IEnumerable{LambdaMetric})) methods to emit custom CloudWatch metrics using the [embedded metric format](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html).
* Added [`ALambdaFunction.SendEvent(...)`](xref:ALambdaFunction.SendEvent(string,object,IEnumerable{string})) method to emit CloudWatch Events to the default event bus on [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/what-is-amazon-eventbridge.html).
* Added `LambdaSharp.Serialization.LambdaJsonSerializer`, which derives from the prescribed JSON serializer (i.e. `Newtonsoft.Json` until a future release).
* Updated embedded CloudFormation spec to 13.0.0.
* Marked `JsonSerializer` property as obsolete. Use `LambdaSerializer` instead.
* Added `--no-ansi` option to `util delete-orphan-logs`, `util download-cloudformation-spec`, and `util create-invoke-methods-schema`.
* Added `util validate-assembly` command.


## New LambdaSharp Core Services Features

* Added metrics to _LambdaSharp.Core_ module.
* Ported module to .NET Core 3.1 with null-aware support.
* Published module with _ReadyToRun_ support for shorter cold-start times.
* Use Kinesis Firehose stream for CloudWatch logs ingestion.
* Store ingested CloudWatch Log events in S3 bucket as queryable JSON records.

### Logging Bucket
> TODO: describe the purpose of the logging bucket

### Create an Athena Table to Query ingested CloudWatch Log Events
```sql
CREATE EXTERNAL TABLE IF NOT EXISTS `<ATHENA-DATABASE>`.Logs (
  `Timestamp` bigint,
  `Type` string,
  `Version` string,
  `ModuleInfo` string,
  `Module` string,
  `ModuleId` string,
  `Function` string,
  `FunctionId` string,
  `Record` string
)
ROW FORMAT SERDE 'org.openx.data.jsonserde.JsonSerDe'
WITH SERDEPROPERTIES (
  'serialization.format' = '1'
) LOCATION 's3://<LOGGING-BUCKET>/logging-success/';
```

### Logging Records
> TODO: describe format for log records


## New Samples

### Samples/EventSample
This sample shows how to use the new [`ALambdaFunction.LogEvent(...)`](xref:ALambdaFunction.LogEvent(string,string,object,IEnumerable{string})) method to emit CloudWatch Events to the default event bus on [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/what-is-amazon-eventbridge.html) method for sending CloudWatch Events.

### Samples/MetricSample
This sample shows how to use the new [`ALambdaFunction.LogMetric(...)`](xref:ALambdaFunction.LogMetric(IEnumerable{LambdaMetric})) methods to emit custom CloudWatch metrics using the [embedded metric format](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html) method for sending CloudWatch Events.

## Bug Fixes

* LambdaSharp CLI
    * Fixed an issue were recreating a _LambdaSharp.Core_ deployment from scratch would not update existing deployed modules with the new deployment bucket name.
    * Let CloudFormation determine the name for `AWS::ApiGateway::Model` resources.
    * Fixed an issue where the `--aws-region` command option didn't always work as expected.
* LambdaSharp Core Module
    * Fixed an issue with processing the Lambda report lines in the CloudWatch logs.

