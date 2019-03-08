![λ#](../../Docs/LambdaSharpLogo.png)

# Module: LambdaSharp.Core
Version 0.5.0.1

## Overview

The `LambdaSharp.Core` module defines the core resources and resource types for deploying λ# modules. This module is included automatically by all λ# modules.

__Topics__
* [Resource Types](#resource-types)
* [Parameters](#parameters)
* [Outputs](#outputs)

## Resource Types
1. [LambdaSharp::Registration::Module](Docs/LambdaSharp-Registration-Module.md)
1. [LambdaSharp::Registration::Function](Docs/LambdaSharp-Registration-Function.md)

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

<dt><code>DefaultSecretKey</code></dt>
<dd>
The <code>DefaultSecretKey</code> parameter sets the default secret key for functions or creates a new key if left blank.

<i>Required</i>: No (Default: create new AWS::KMS::Key)

<i>Type:</i> AWS::KMS::Key
</dd>

<dt><code>DefaultSecretKeyRotationEnabled</code></dt>
<dd>
The <code>DefaultSecretKeyRotationEnabled</code> parameter enables rotating KMS key automatically every 365 days.

<i>Required</i>: No (Default: false)

<i>Type:</i> String (either <code>true</code> or <code>false</code>)

</dd>

</dl>

### Rollbar Settings

The following settings are required to use the [Rollbar](https://rollbar.com/) integration for the λ# Core module.

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

<dt><code>DefaultSecretKey</code></dt>
<dd>
The <code>DefaultSecretKey</code> output contains the default secret key for functions.

<i>Type:</i> AWS::KMS::Key
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

