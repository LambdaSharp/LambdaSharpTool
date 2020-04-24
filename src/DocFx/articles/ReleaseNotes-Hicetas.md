---
title: LambdaSharp "Hicetas" Release (v0.8)
description: Release notes for LambdaSharp "Hicetas" (v0.8)
keywords: release, notes, hicetas
---

# LambdaSharp "Hicetas" Release (v0.8.0.0) - TBD

> Hicetas was a Greek philosopher of the Pythagorean School. He was born in Syracuse. Like his fellow Pythagorean Ecphantus and the Academic Heraclides Ponticus, he believed that the daily movement of permanent stars was caused by the rotation of the Earth around its axis. When Copernicus referred to Nicetus Syracusanus (Nicetus of Syracuse) in _De revolutionibus orbium coelestium_ as having been cited by Cicero as an ancient who also argued that the Earth moved, it is believed that he was actually referring to Hicetas. [(Wikipedia)](https://en.wikipedia.org/wiki/Hicetas)

## What's New

TODO:

* LambdaSharp SDK
    * Ported `LambdaSharp` assembly to .NET Core 3.1 with null-aware support.

## BREAKING CHANGES

* Switched to `System.Text.Json` for serialization.
    * Replace `[JsonProperty]` with `[JsonPropertyName()]` (requires `using System.Text.Json.Serialization;`).
    * Replace `[JsonRequired]` with `[DataMember(IsRequired = true)]` (requires `using System.Runtime.Serialization;`).
    * Replace `[JsonProperty(Required = Required.DisallowNull)]` with `[DataMember(IsRequired = true)]` (requires `using System.Runtime.Serialization;`).
    * Replace `<TargetFramework>netcoreapp2.1</TargetFramework>` with `<TargetFramework>netcoreapp3.1</TargetFramework>`.
    * Replace `[JsonConverter(typeof(JsonStringEnumConverter))]` with `[JsonConverter(typeof(JsonStringEnumConverter))]` (requires `using System.Text.Json.Serialization;`).
    * Replace `SerializeJson` with `LambdaSerializer.Serialize`.
    * Replace `DeserializeJson` with `LambdaSerializer.Serialize`.
    * Replace fields with properties! (SUPER IMPORTANT!!!)
    * Beware of `string` properties to deserialize a JSON number. That won't work anymore.
* Removed `SerializeJson()` and `DeserializeJson()`; use `LambdaSerialize.Serialize()` and `LambdaSerializer.Deserialize()` respectively.
* Removed `Newtonsoft.Json` dependency from `LambdaSharp.dll`
* Replace `[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]` with `[assembly: LambdaSerializer(typeof(LambdaSharp.Serialization.LambdaJsonSerializer))]`


## New LambdaSharp CLI Features

## New LambdaSharp Assembly Features

### LambdaSharp.Core
    ...

### (v0.8.0.0) - TBD

**IMPORTANT:** In preparation of switching `LambdaSharp` assembly to .NET Core 3.1 and using `System.Text.Json` in a future release, it is highly advised to remove all references to `Amazon.Lambda.Serialization.Json` and `Newtonsoft.Json` from all _.csproj_ files. These package are currently inherited via the `LambdaSharp` assembly anyway. This change will ensure that these obsolete references are no longer included once a project is upgraded to using the future release of `LambdaSharp`. Additionally, the default assembly serializer should be switched from `Amazon.Lambda.Serialization.Json.JsonSerializer` to `LambdaSharp.Serialization.LambdaJsonSerializer`.

#### New Features

* LambdaSharp SDK
    * Added [`ALambdaFunction.LogMetric(...)`](xref:ALambdaFunction.LogMetric(IEnumerable{LambdaMetric})) methods to emit custom CloudWatch metrics using the [embedded metric format](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html).
    * Added [`ALambdaFunction.LogEvent(...)`](xref:ALambdaFunction.LogEvent(string,string,object,IEnumerable{string})) method to emit CloudWatch Events to the default event bus on [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/what-is-amazon-eventbridge.html).
    * Added `LambdaSharp.Serialization.LambdaJsonSerializer`, which derives from the prescribed JSON serializer (i.e. `Newtonsoft.Json` until a future release).
    * Updated embedded CloudFormation spec to 13.0.0.
    * Added CloudWatch metrics to `ALambdaQueueFunction<T>` base class.
    * Added CloudWatch metrics to `ALambdaTopicFunction<T>` base class.
    * Marked `JsonSerializer` property as obsolete. Use `LambdaSerializer` instead.
    * Marked `SerializeJson()` and `DeserializeJson()` methods as obsolete. Use `LambdaSerialize.Serialize()` and `LambdaSerializer.Deserialize()` respectively.
* LambdaSharp Core Module
    * Added metrics to _LambdaSharp.Core_ module.
    * Ported `LambdaSharp.Core` to .NET Core 3.1 with null-aware support.
    * Published `LambdaSharp.Core` module with _ReadyToRun_ support for shorter cold-start times.
* Samples
    * `Samples/EventSample` shows how to use the new [`ALambdaFunction.LogEvent(...)`](xref:ALambdaFunction.LogEvent(string,string,object,IEnumerable{string})) method to emit CloudWatch Events to the default event bus on [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/what-is-amazon-eventbridge.html) method for sending CloudWatch Events.
    * `Samples/MetricSample` shows how to use the new [`ALambdaFunction.LogMetric(...)`](xref:ALambdaFunction.LogMetric(IEnumerable{LambdaMetric})) methods to emit custom CloudWatch metrics using the [embedded metric format](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html) method for sending CloudWatch Events.

#### Fixes

* LambdaSharp CLI
    * Fixed an issue were recreating a _LambdaSharp.Core_ deployment from scratch would not update existing deployed modules with the new deployment bucket name.
    * Removed package reference to `Amazon.Lambda.Serialization.Json` from all _.csproj_ files as this package is inherited via the `LambdaSharp` reference.
    * Removed package reference to `Newtonsoft.Json` from all _.csproj_ files as this package is inherited via the `LambdaSharp` reference.
    * Let CloudFormation determine the name for `AWS::ApiGateway::Model` resources.
* LambdaSharp Core Module
    * Fixed an issue with processing the Lambda report lines in the CloudWatch logs.

