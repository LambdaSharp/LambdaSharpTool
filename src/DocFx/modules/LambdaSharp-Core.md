---
title: LambdaSharp.Core - LambdaSharp Module
description: Documentation for LambdaSharp.Core module
keywords: module, core, documentation, overview
---

# Module: LambdaSharp.Core
_Version:_ [!include[LAMBDASHARP_VERSION](../version.txt)]

## Overview

The `LambdaSharp.Core` module defines the core resources and resource types for deploying LambdaSharp modules. This module is included automatically by all LambdaSharp modules.

## Resource Types
1. [LambdaSharp::Registration::Module](LambdaSharp-Registration-Module.md)
1. [LambdaSharp::Registration::Function](LambdaSharp-Registration-Function.md)

## Parameters

### LambdaSharp Tier Settings

<dl>

<dt><code>DeadLetterQueue</code></dt>
<dd>

The <code>DeadLetterQueue</code> parameter sets the Dead letter queue for functions or creates a new queue if left blank.

<i>Required</i>: No (Default: Create new AWS::SQS::Queue)

<i>Type:</i> AWS::SQS::Queue
</dd>

<dt><code>LoggingStream</code></dt>
<dd>

The <code>LoggingStream</code> parameter sets the Logging Kinesis stream for functions or creates a new stream if left blank.

<i>Required</i>: No (Default: create new AWS::Kinesis::Stream)

<i>Type:</i> AWS::Kinesis::Stream
</dd>

<dt><code>LoggingStreamRetentionPeriodHours</code></dt>
<dd>

The <code>LoggingStreamRetentionPeriodHours</code> parameter sets the size of the Logging stream buffer (in hours).

<i>Required</i>: No (Default: 24)

<i>Type:</i> Number
</dd>

<dt><code>LoggingStreamShardCount</code></dt>
<dd>

The <code>LoggingStreamShardCount</code> parameter sets the number of Kinesis shards for the logging streams.

<i>Required</i>: No (Default: 1)

<i>Type:</i> Number
</dd>

</dl>

### Rollbar Settings

The following settings are required to use the [Rollbar](https://rollbar.com/) integration for the LambdaSharp Core module.

<dl>

<dt><code>RollbarReadAccessToken</code></dt>
<dd>

The <code>RollbarReadAccessToken</code> parameter sets account-level token for read operations (keep blank to disable Rollbar integration). This parameter must either be encrypted with the default deployment tier KMS key, or the corresponding KMS key must be passed in via  the <code>Secrets</code> parameter.

<i>Required</i>: No

<i>Type:</i> Secret

</dd>

<dt><code>RollbarWriteAccessToken</code></dt>
<dd>

The <code>RollbarWriteAccessToken</code> parameter sets account-level token for write operations (keep blank to disable Rollbar integration). This parameter must either be encrypted with the default deployment tier KMS key, or the corresponding KMS key must be passed in via  the <code>Secrets</code> parameter.

<i>Required</i>: No

<i>Type:</i> Secret

</dd>

<dt><code>RollbarProjectPrefix</code></dt>
<dd>

The <code>RollbarProjectPrefix</code> parameter sets optional prefix when creating Rollbar projects (e.g. "LambdaSharp-").

<i>Required</i>: No

<i>Type:</i> String

</dd>

</dl>

## Output Values

<dl>

<dt><code>DeadLetterQueue</code></dt>
<dd>

The <code>DeadLetterQueue</code> output contains the dead letter queue for functions.

<i>Type:</i> AWS::SQS::Queue
</dd>

<dt><code>LoggingStream</code></dt>
<dd>

The <code>LoggingStream</code> output contains the logging Kinesis stream for functions.

<i>Type:</i> AWS::Kinesis::Stream
</dd>

<dt><code>LoggingStreamRole</code></dt>
<dd>

The <code>LoggingStreamRole</code> output contains the IAM role used by CloudWatch logs to write to the Kinesis stream.

<i>Type:</i> AWS::IAM::Role
</dd>

<dt><code>ErrorReportTopic</code></dt>
<dd>

The <code>ErrorReportTopic</code> output contains the SNS topic for LambdaSharp module errors.

<i>Type:</i> AWS::SNS::Topic
</dd>

<dt><code>UsageReportTopic</code></dt>
<dd>

The <code>UsageReportTopic</code> output contains the SNS topic for LambdaSharp function usage reports.

<i>Type:</i> AWS::SNS::Topic
</dd>

</dl>

