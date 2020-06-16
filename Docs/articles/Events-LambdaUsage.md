---
title: LambdaUsage Event - LambdaSharp Operational Events - LambdaSharp
description: Description of Lambda invocation usage event emitted by LambdaSharp.Core
keywords: cloudwatch, events, modules, lambda, invocation, usage
---

# LambdaUsage Event

The _LambdaUsage_ event is emitted by _LambdaSharp.Core_ when ingesting a Lambda invocation report from the associated the CloudWatch logs. Note that _Core Services_ must be enabled for these events to be emitted.

## Event Schema

```yaml
Type: String # UsageReport
Version: String # 2020-05-05
ModuleInfo: String
FunctionId: String
ModuleId: String
Module: String
Function: String
BilledDuration: Float
UsedDuration: Float
UsedDurationPercent: Float
MaxDuration: Float
MaxMemory: Int
UsedMemory: Int
UsedMemoryPercent: Float
InitDuration: Float?
```

## Event Properties

<dl>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> property holds the event type name. This property is always set to <code>"LambdaUsage"</code>.

<i>Type</i>: String
</dd>

<dt><code>Version</code></dt>
<dd>

The <code>Version</code> property holds the event type version. This property is set to <code>"2020-05-05"</code>.

<i>Type</i>: String
</dd>

<dt><code>ModuleInfo</code></dt>
<dd>

The <code>ModuleInfo</code> property holds the module full name, version, and origin.

<i>Type</i>: String
</dd>

<dt><code>FunctionId</code></dt>
<dd>

The <code>FunctionId</code> property holds the resource name of the Lambda function.

<i>Type</i>: String
</dd>

<dt><code>ModuleId</code></dt>
<dd>

The <code>ModuleId</code> property holds the CloudFormation stack ID.

<i>Type</i>: String
</dd>

<dt><code>Module</code></dt>
<dd>

The <code>Module</code> property holds the module full name.

<i>Type</i>: String
</dd>

<dt><code>Function</code></dt>
<dd>

The <code>Function</code> property holds the logical function name as defined by the module.

<i>Type</i>: String
</dd>

<dt><code>BilledDuration</code></dt>
<dd>

The <code>BilledDuration</code> property holds the number of milliseconds the AWS account will be billed for this Lambda function invocation.

<i>Type</i>: Float
</dd>

<dt><code>UsedDuration</code></dt>
<dd>

The <code>UsedDuration</code> property holds the execution time for the Lambda function invocation in milliseconds. The invocation time does not include the cold start duration, which is tracked independently by the <code>InitDuration</code> property.

<i>Type</i>: Float
</dd>

<dt><code>UsedDurationPercent</code></dt>
<dd>

The <code>UsedDurationPercent</code> property holds the execution time for the Lambda function invocation normalized to the maximum allowed duration. The value is always between 0.0 and 1.0.

<i>Type</i>: Float
</dd>

<dt><code>MaxDuration</code></dt>
<dd>

The <code>MaxDuration</code> property holds the maximum execution time allowed for a Lambda function invocation. This execution limit does not include the cold start duration.

<i>Type</i>: Float
</dd>

<dt><code>MaxMemory</code></dt>
<dd>

The <code>MaxMemory</code> property holds the maximum amount of memory usable by the Lambda function in MB.

<i>Type</i>: Int
</dd>

<dt><code>UsedMemory</code></dt>
<dd>

The <code>UsedMemory</code> property holds the used amount of memory during the Lambda function invocation in MB.

<i>Type</i>: Int
</dd>

<dt><code>UsedMemoryPercent</code></dt>
<dd>

The <code>UsedMemoryPercent</code> property holds the used memory amount the Lambda function invocation normalized to the maximum allowed amount. The value is always between 0.0 and 1.0.

<i>Type</i>: Float
</dd>

<dt><code>InitDuration</code></dt>
<dd>

The <code>InitDuration</code> property holds the startup time required by a Lambda function in milliseconds. This property is only set for a Lambda function that required a cold start, otherwise the property is <code>null</code>.

<i>Type</i>: Float or <code>null</code>
</dd>

</dl>

## Event Sample

```json
{
  "ModuleInfo": "LambdaSharp.Twitter.Query:0.7.1.0",
  "FunctionId": "Sandbox-Demo-TwitterNotifier-Twitte-QueryFunction-13VL7ZV8BTWKN",
  "ModuleId": "Sandbox-Demo-TwitterNotifier-TwitterNotify-1F8924TBHKNJ9",
  "Module": "LambdaSharp.Twitter.Query",
  "Function": "QueryFunction",
  "BilledDuration": 12.5,
  "UsedDuration": 12.46635,
  "UsedDurationPercent": 0.415545,
  "MaxDuration": 30.0,
  "MaxMemory": 256,
  "UsedMemory": 109,
  "UsedMemoryPercent": 0.42578125,
  "InitDuration": 0.43456,
  "Type": "LambdaUsage",
  "Version": "2020-05-05"
}
```
