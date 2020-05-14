---
title: LambdaMetrics Event - LambdaSharp Operational Events LambdaSharp
description: Description of custom CloudWatch metrics event emitted by LambdaSharp.Core
keywords: cloudwatch, events, modules
---

# LambdaMetrics Event

The _LambdaMetrics_ event is emitted by _LambdaSharp.Core_ when ingesting embedded CloudWatch metrics from the CloudWatch logs associated with the Lambda function. Note that _Core Services_ must be enabled for these events to be emitted. The _LambdaMetrics_ schema is based on the [CloudWatch Embedded Metric format](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format_Specification.html). It has the same structure with the additional of the following properties.

## Event Schema

```yaml
Type: String
Version: String
Stack: String
Function: String
GitSha: String?
GitBranch: String?
# CloudWatch Embedded Metric Properties
```

## Event Properties

<dl>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> property holds the event type name. This property is always set to <code>"LambdaMetrics"</code>.

<i>Type</i>: String
</dd>

<dt><code>Version</code></dt>
<dd>

The <code>Version</code> property holds the event type version. This property is set to <code>"2020-05-05"</code>.

<i>Type</i>: String
</dd>

<dt><code>Stack</code></dt>
<dd>

The <code>Stack</code> property holds the CloudFormation stack ID containing the Lambda function.

<i>Type</i>: String
</dd>

<dt><code>Function</code></dt>
<dd>

The <code>Function</code> property holds logical Lambda function name as defined by its module.

<i>Type</i>: String
</dd>

<dt><code>GitSha</code></dt>
<dd>

The <code>GitSha</code> property holds the git SHA checksum when the module was deployed from a git repository, otherwise <code>null</code>.

<i>Type</i>: String or <code>null</code>
</dd>

<dt><code>GitBranch</code></dt>
<dd>

The <code>GitBranch</code> property holds the git branch name when the module was deployed from a git repository, otherwise <code>null</code>.

<i>Type</i>: String or <code>null</code>
</dd>

</dl>

## Event Sample

```json
{
  "_aws": {
    "Timestamp": 1589220705881,
    "CloudWatchMetrics": [
      {
        "Namespace": "Module:Sample.Metric",
        "Dimensions": [["Stack"], ["Stack", "Function"]],
        "Metrics": [
          { "Name": "CompletedMessages.Latency", "Unit": "Milliseconds" },
          { "Name": "CompletedMessages.Count", "Unit": "Count" }
        ]
      }
    ]
  },
  "Type": "LambdaMetrics",
  "Version": "2020-05-05",
  "CompletedMessages.Latency": 100.0,
  "CompletedMessages.Count": 1.0,
  "Stack": "Sandbox-Sample-Metric",
  "Function": "MyFunction",
  "GitSha": "DIRTY-07ed927da2d1f7fea61a1472b14e647506c3bbc7",
  "GitBranch": "WIP-v0.8"
}
```
