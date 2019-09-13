---
title: S3 Notification Event Source Declaration - Function
description: LambdaSharp YAML syntax for Amazon S3 notification event source
keywords: amazon, s3, notification, event source, declaration, lambda, syntax, yaml, cloudformation
---
# S3 Source

See [S3 Bucket sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/S3Sample/) for an example of how to use the S3 Bucket source.

## Syntax

```yaml
S3: String|Expression
Events: [ String ]
Prefix: String
Suffix: String
```

## Properties

<dl>

<dt><code>Events</code></dt>
<dd>

The <code>Events</code> section specifies the S3 events that trigger an invocation of the Lambda function. By default, the Lambda function only reacts to <code>s3:ObjectCreated:*</code> events. See <a href="https://docs.aws.amazon.com/AmazonS3/latest/dev/NotificationHowTo.html#notification-how-to-event-types-and-destinations">S3 Event Notification Types and Destinations</a> for a complete list of S3 events.

<i>Required</i>: No

<i>Type</i>: List of String
</dd>

<dt><code>Prefix</code></dt>
<dd>

The <code>Prefix</code> attribute specifies a filter to limit invocations to object key names that begin with the attribute value.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>S3</code></dt>
<dd>

The <code>S3</code> attribute specifies the name of a resource parameter of type <code>AWS::S3::Bucket</code> that is the origin of the events.

<i>Required</i>: Yes

<i>Type</i>: String or Expression
</dd>

<dt><code>Suffix</code></dt>
<dd>

The <code>Suffix</code> attribute specifies a filter to limit invocations to object key names that end with the attribute value.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>

## Examples

Listen to `s3:ObjectCreated:*` on the S3 bucket.

```yaml
Sources:
  - S3: MyFirstBucket
```

Listen to custom events on the S3 bucket for specific S3 keys.

```yaml
Sources:
  - S3: MySecondBucket
    Events:
      - "s3:ObjectCreated:*"
      - "s3:ObjectRemoved:*"
    Prefix: images/
    Suffix: .png
```
