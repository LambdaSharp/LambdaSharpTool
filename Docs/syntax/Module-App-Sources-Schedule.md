---
title: CloudWatch Scheduled Events Event Source Declaration - App
description: LambdaSharp YAML syntax for Amazon CloudWatch Scheduled Events event source
keywords: amazon, cloudwatch, schedule, events, event source, declaration, app, syntax, yaml, cloudformation
---
# Schedule Source

See [Blazor Event sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/BlazorEventSample/) for an example of how to use the CloudWatch Event source with a LambdaSharp app.

## Syntax

```yaml
Schedule: String|Expression
Name: String
```

## Properties

<dl>

<dt><code>Name</code></dt>
<dd>

The <code>Name</code> attribute specifies a name for this CloudWatch Schedule Event to distinguish it from other CloudWatch Schedule Event sources.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Schedule</code></dt>
<dd>

The <code>Schedule</code> attribute specifies a <code>cron</code> or <code>rate</code> expression that defines a schedule for regularly invoking the Lambda function. See <a href="https://docs.aws.amazon.com/lambda/latest/dg/tutorial-scheduled-events-schedule-expressions.html">Schedule Expressions Using Rate or Cron</a> for more information.

<i>Required</i>: Yes

<i>Type</i>: String or Expression
</dd>

</dl>

## Examples

Define a schedule event source to publish a recurring event to the LambdaSharp App EventBus using a `cron` expression.

```yaml
Sources:
  - Schedule: cron(0 12 * * ? *)
```

Define a named schedule event source using a `rate` expression.

```yaml
Sources:
  - Schedule: rate(1 minute)
    Name: MyEvent
```
