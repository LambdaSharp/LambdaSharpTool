---
title: LambdaSharp::S3::Subscription - LambdaSharp.S3.Subscriber Module
description: Documentation for LambdaSharp::S3::Subscription resource type
keywords: module, documentation, resource, type, properties, attributes, s3, subscription, notification
---

# LambdaSharp::S3::Subscription

The `LambdaSharp.S3.Subscriber` module defines the `LambdaSharp::S3::Subscription` resource type, which is automatically used by the LambdaSharp CLI to subscribe Lambda functions to S3 events.

## Using

> **NOTE:** the LambdaSharp CLI automatically adds the required `Using` statement when a Lambda function subscribes to S3 events.

```yaml
Using:

  - Module: LambdaSharp.S3.Subscriber:0.5
```

## Syntax

```yaml
Type: LambdaSharp::S3::Subscription
Properties:
  Bucket: String
  Function: String
  Filters:
    - FilterDefinition
```

## Properties

<dl>

<dt><code>Bucket</code></dt>
<dd>

The <code>Bucket</code> property specifies the S3 bucket name or ARN to subscribe to.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Function</code></dt>
<dd>

The <code>Function</code> property specifies the Lambda ARN to invoke with the events.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Filters</code></dt>
<dd>

The <code>Filters</code> property specifies the list of filters for S3 events to subscribe to.

<i>Required</i>: Yes

<i>Type</i>: List of <code>FilterDefinition</code>

</dd>

</dl>

## Attributes

<dl>

<dt><code>Result</code></dt>
<dd>

The <code>Result</code> attribute contains the S3 URL of the bucket.

<i>Type</i>: String
</dd>

</dl>

## Types

### `LambdaSharp::S3::Subscription.FilterDefinition`

<dl>

<dt><code>Events</code></dt>
<dd>

The <code>Events</code> property specifies the list of events to subscribe to.

<i>Required</i>: Yes

<i>Type</i>: List<String>
</dd>

<dt><code>Prefix</code></dt>
<dd>

The <code>Prefix</code> property specifies a case-sensitive filter that must match the beginning of the S3 key.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Suffix</code></dt>
<dd>

The <code>Suffix</code> property specifies a case-sensitive filter that must match the end of the S3 key.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>


## Examples

### Manually create an S3 event subscription

```yaml
- Resource: MyBucket
  Type: AWS::S3::Bucket

- Function: MyFunction
  Memory: 256
  Timeout: 30

- Resource: MyBucketSubscription
  Type: LambdaSharp::S3::Subscription
  Properties:
    Bucket: !Ref MyBucket
    Function: !RFef MyFunction
    Filters:

      - Events:
          - s3:ObjectCreated:*
        Prefix: inbox/
        Suffix: .png
```
