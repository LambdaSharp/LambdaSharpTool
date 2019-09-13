---
title: LambdaSharp Samples
description: List of LambdaSharp samples
keywords: tutorial, sample, example, getting started, overview
---

# LambdaSharp Samples

## REST API using API Gateway V1

API Gateway allows you to build a fully managed, serverless REST API. API endpoints are automatically validated by API Gateway before invoking an attached Lambda function.

**Related**:
* Base class: [LambdaSharp.ApiGateway.ALambdaApiGatewayFunction](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction)
* Sample: [ApiSample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/ApiSample)

## Web Sockets using API Gateway V2

API Gateway V2 allows you to build a fully managed, serverless Web Socket API. The WebSocket routes are automatically validated by API Gateway V2 before invoking an attached Lambda function.

**Related**:
* Base class: [LambdaSharp.ApiGateway.ALambdaApiGatewayFunction](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction)
* Sample: [WebSocketChat-Sample](https://github.com/LambdaSharp/WebSocketsChat-Sample)

## SNS Topic

The Simple Notification Service (SNS) is provides pub-sub capabilities at scale. The base class handles deserialization of incoming messages and automatic handling of permanent failures.

**Related**:
* Base class: [LambdaSharp.SimpleNotificationService.ALambdaTopicFunction&lt;TMessage&gt;](xref:LambdaSharp.SimpleNotificationService.ALambdaTopicFunction`1)
* Sample: [SnsSample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/SnsSample)

## SQS Queue

The Simple Queue Service (SQS) is provides reliable, discrete message delivery. The base class handles deserialization of incoming messages, automatic handling of permanent failures, and improved handling of partial failures for batched messages.

**Related**:
* Base class: [LambdaSharp.SimpleQueueService.ALambdaQueueFunction&lt;TMessage&gt;](xref:LambdaSharp.SimpleQueueService.ALambdaQueueFunction`1)
* Sample: [SqsSample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/SqsSample)

## Scheduled Event

A common use-case for Lambda functions is repeating a task at regular intervals. The included base class makes it easy to handle multiple scheduled events at once.

**Related**:
* Base class: [LambdaSharp.Schedule.ALambdaScheduleFunction](xref:LambdaSharp.Schedule.ALambdaScheduleFunction)
* Sample: [ScheduleSample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/ScheduleSample)

## Finalizer

The _Finalizer_ is a function that is run automatically a part CloudFormation stack operation. It is used to initialize resources as part of the CloudFormation stack creation or to tear-down/clean-up resources before the stack is deleted.

**Related**:
* Base class: [LambdaSharp.Finalizer.ALambdaFinalizerFunction](xref:LambdaSharp.Finalizer.ALambdaFinalizerFunction)
* Sample: [FinalizerSample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/FinalizerSample)

## Custom Resource Type

Define your own resource types in CloudFormation. The base class provides the necessary protocol and error handling for CloudFormation.

**Related**:
* Base class: [LambdaSharp.CustomResource.ALambdaCustomResourceFunction&lt;TProperties,TAttributes&gt;](xref:Lambda* harp.CustomResource.ALambdaCustomResourceFunction`2)

Sample: [CustomResourceTypeSample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/CustomResourceTypeSample)
