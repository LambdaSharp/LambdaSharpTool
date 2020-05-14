---
title: Sample Applications and Patterns - LambdaSharp
description: List of LambdaSharp samples
keywords: tutorial, sample, example, getting started, overview
---

# Sample Applications

## Create Static Website with CloudFormation

This LambdaSharp module creates a static website hosted by an S3 bucket with a customizable title and greeting message. The assets for the website are uploaded from the `wwwroot` folder and copied to the S3 bucket during deployment.

Sample: [StaticWebsite-Sample](https://github.com/LambdaSharp/StaticWebsite-Sample)

## Create Animated GIFs from Videos with AWS Lambda

This LambdaSharp module creates a Lambda function that converts videos to animated GIFs. The conversion is done by the [FFmpeg application](https://www.ffmpeg.org/) which is deployed as a [Lambda Layer](https://docs.aws.amazon.com/lambda/latest/dg/configuration-layers.html). The module uses two S3 buckets: one for uploading videos and one for storing the converted animated GIFs. The Lambda function is automatically invoked when a file is uploaded the video S3 bucket.

**Related**:
* Sample: [GifMaker-Sample](https://github.com/LambdaSharp/GifMaker-Sample)

## Create a Web Chat with API Gateway WebSockets

This LambdaSharp module creates a web chat front-end and back-end using CloudFormation. The front-end is served by an S3 bucket and secured by a CloudFront distribution. The back-end uses API Gateway Websockets to facilitate communication between clients. The assets for the front-end are uploaded from the `wwwroot` folder and copied to the S3 bucket during deployment. Afterwards, a CloudFront distribution is created to provide secure access over `https://` to the front-end. In addition, an API Gateway (v2) is deployed with two Lambda functions that handle websocket connections and message notifications.

**Related**:
* Sample: [WebSocketsChat-Sample](https://github.com/LambdaSharp/WebSocketsChat-Sample)

## λ-Robots Game

In λ-Robots (pronounced _Lambda Robots_), you program a battle robot that participates on a square game field. Each turn, the server invokes your robot's Lambda function to get its action for the turn until either the robot wins or is destroyed.

**Related**:
* Sample: [LambdaRobots](https://github.com/LambdaSharp/LambdaRobots)

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

The Simple Notification Service (SNS) provides pub-sub capabilities at scale. The base class handles deserialization of incoming messages and automatic handling of permanent failures.

**Related**:
* Base class: [LambdaSharp.SimpleNotificationService.ALambdaTopicFunction&lt;TMessage&gt;](xref:LambdaSharp.SimpleNotificationService.ALambdaTopicFunction`1)
* Sample: [SnsSample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/SnsSample)

## SQS Queue

The Simple Queue Service (SQS) provides reliable, discrete message delivery. The base class handles deserialization of incoming messages, automatic handling of permanent failures, and improved handling of partial failures for batched messages.

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

## Misc. λ# Samples

Various module samples showcasing LambdaSharp declarations and patterns.

**Related**:
* Samples: [LambdaSharp GitHub Samples](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/ReadMe.md)