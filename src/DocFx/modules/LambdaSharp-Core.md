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

<dt><code>CoreSecretsKey</code></dt>
<dd>

The <code>CoreSecretsKey</code> parameter sets the KMS key used by LambdaSharp.Core to encrypt sensitive information. If left blank, a new key is created.

<i>Required</i>: No (Default: Create new AWS::KMS::Key)

<i>Type:</i> AWS::KMS::Key
</dd>

<dt><code>DeadLetterQueue</code></dt>
<dd>

The <code>DeadLetterQueue</code> parameter sets the Dead letter queue for functions. If left blank, a new queue is created.

<i>Required</i>: No (Default: Create new AWS::SQS::Queue)

<i>Type:</i> AWS::SQS::Queue
</dd>

<dt><code>LoggingBucket</code></dt>
<dd>

The <code>LoggingBucket</code> parameter sets the S3 bucket for storing ingested log entries. If left blank, a new bucket is created.

<i>Required</i>: No (Default: Create new AWS::S3::Bucket)

<i>Type:</i> AWS::S3::Bucket
</dd>

<dt><code>LoggingBucketSuccessPrefix</code></dt>
<dd>

The <code>LoggingBucketSuccessPrefix</code> parameter sets the destination S3 bucket prefix for records successfully processed by the logging stream.

<i>Required</i>: No (Default: <code>logging-success/</code>)

<i>Type:</i> String
</dd>

<dt><code>LoggingBucketFailurePrefix</code></dt>
<dd>

The <code>LoggingBucketFailurePrefix</code> parameter sets the destination S3 bucket prefix for records unsuccessfully processed processed by the logging stream.

<i>Required</i>: No (Default: <code>logging-failed/</code>)

<i>Type:</i> String
</dd>

<dt><code>LoggingFirehoseStream</code></dt>
<dd>

The <code>LoggingFirehoseStream</code> parameter sets the Logging Kinesis Firehose stream for functions. If left blank, a new stream is created.

<i>Required</i>: No (Default: Create new AWS::KinesisFirehose::DeliveryStream)

<i>Type:</i> AWS::KinesisFirehose::DeliveryStream
</dd>

<dt><code>LoggingStreamRole</code></dt>
<dd>

The <code>LoggingStreamRole</code> parameter sets the IAM role used by CloudWatch Logs to write records to the logging stream. If left blank, a new role is created.

<i>Required</i>: No (Default: Create new AWS::IAM::Role)

<i>Type:</i> AWS::IAM::Role
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

<dt><code>DeploymentBucket</code></dt>
<dd>

The <code>DeploymentBucket</code> output contains the S3 bucket for the deployment tier artifacts.

<i>Type:</i> AWS::S3::Bucket
</dd>

<dt><code>LoggingBucket</code></dt>
<dd>

The <code>LoggingBucket</code> output contains the S3 bucket for processed log entries.

<i>Type:</i> AWS::S3::Bucket
</dd>

<dt><code>LoggingStream</code></dt>
<dd>

The <code>LoggingStream</code> output contains the logging Kinesis stream for functions.

<i>Type:</i> AWS::KinesisFirehose::DeliveryStream
</dd>

<dt><code>LoggingStreamRole</code></dt>
<dd>

The <code>LoggingStreamRole</code> output contains the IAM role used by CloudWatch logs to write to the Kinesis stream.

<i>Type:</i> AWS::IAM::Role
</dd>

</dl>

