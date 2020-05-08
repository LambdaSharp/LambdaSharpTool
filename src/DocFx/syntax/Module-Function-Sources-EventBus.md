---
title: CloudWatch Event Source Declaration - Function
description: LambdaSharp YAML syntax for Amazon CloudWatch Topic event source
keywords: amazon, cloudwatch, events, event bus, event source, declaration, lambda, syntax, yaml, cloudformation
---
# CloudWatch Event Source

See [CloudWatch Event sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/EventSample/) for an example of how to use the CloudWatch Event source.

## Syntax

```yaml
EventBus: String|Expression
Pattern: JSON
```

## Properties

<dl>

<dt><code>EventBus</code></dt>
<dd>

The <code>EventBus</code> attribute specifies which event bus to listen to.

<i>Required</i>: Yes

<i>Type</i>: String or Expression
</dd>

<dt><code>Pattern</code></dt>
<dd>

The <code>Pattern</code> attribute describes which events are routed to the Lambda function. For more information, see <a href="https://docs.aws.amazon.com/eventbridge/latest/userguide/eventbridge-and-event-patterns.html">Events and Event Patterns in EventBridge</a> in the Amazon EventBridge User Guide.

<i>Required</i>: Yes

<i>Type</i>: JSON
</dd>

</dl>

## Examples

### Receive Events from Deployment Tier

The following definition receives events from `Sample.Event` module of type `MyFirstEvent`, but only when sent from the same deployment tier.

```yaml
Sources:
  - EventBus: default
    Pattern:
      source: [ Sample.Event ]
      detail-type: [ MyFirstEvent ]
      resources:
        - !Sub "lambdasharp:tier:${Module::Tier}"
```
