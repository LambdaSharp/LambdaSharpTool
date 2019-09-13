---
title: LambdaSharp Modules
description: List of LambdaSharp modules
keywords: module, overview
---

![λ#](~/images/Cloud.png)

# LambdaSharp Modules

The following standard LambdaSharp modules are available to all modules.

## LambdaSharp.Core

This the [LambdaSharp.Core](LambdaSharp-Core.md) module for a LambdaSharp deployment tier. It is automatically included with every module.

## LambdaSharp.S3.IO

The [LambdaSharp.S3.IO](LambdaSharp-S3-IO.md) module defines resources types for writing files and unzipping archives to S3 buckets.

## LambdaSharp.S3.Subscriber

The [LambdaSharp.S3.Subscriber](LambdaSharp-S3-Subscriber.md) module defines a resource for subscribing to S3 bucket events. This module is automatically referenced by module with Lambda functions reacting to S3 events.

## LambdaSharp.Twitter.Query

The [LambdaSharp.Twitter.Query](LambdaSharp-Twitter-Query.md) module runs a Twitter query at regular intervals and sends the query results to an SNS topic, which can be listened to by a Lambda function.