![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - ??? Function Source

See [CloudWatch Schedule Event sample](../Samples/ScheduleSample/) for an example of how to use the CloudWatch Schedule Event source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Schedule: String
Name: String
```

## Properties

<dl>
<dt><code>Schedule</code></dt>
<dd>
The <code>Schedule</code> attribute specifies a <code>cron</code> or <code>rate</code> expression that defines a schedule for regularly invoking the Lambda function. See <a href="https://docs.aws.amazon.com/lambda/latest/dg/tutorial-scheduled-events-schedule-expressions.html">Schedule Expressions Using Rate or Cron</a> for more information.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Name</code></dt>
<dd>
The <code>Name</code> attribute specifies a name for this CloudWatch Schedule Event to distinguish it from other CloudWatch Schedule Event sources.

<i>Required</i>: No

<i>Type</i>: String
</dd>
</dl>

## Examples

Define a schedule event to invokes an associated Lambda function using a `cron` expression.

```yaml
Sources:
  - Schedule: cron(0 12 * * ? *)
```

Define a named schedule event using a `rate` expression.

```yaml
Sources:
  - Schedule: rate(1 minute)
    Name: MyEvent
```
