---
title: LambdaSharp::S3::EmptyBucket - LambdaSharp.S3.IO Module
description: Documentation for LambdaSharp::S3::EmptyBucket resource type
keywords: module, s3, io, documentation, resource, type, properties, attributes, empty, bucket
---

# LambdaSharp::S3::EmptyBucket

The `LambdaSharp::S3::EmptyBucket` type creates a resource that empties the attached S3 bucket when the resource is deleted.

On creation or update, nothing happens. On the deletion, the contents of the S3 bucket will be deleted.

## Using

```yaml
Using:

  - Module: LambdaSharp.S3.IO:0.5@lambdasharp
```

## Syntax

```yaml
Type: LambdaSharp::S3::EmptyBucket
Properties:
  Bucket: String
  Enabled: Boolean
```

## Properties

<dl>

<dt><code>Bucket</code></dt>
<dd>

The <code>Bucket</code> property specifies the S3 bucket name or ARN to empty.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Enabled</code></dt>
<dd>

The <code>Enabled</code> property controls if the bucket will be emptied when the resource is deleted. Setting the <code>Enabled</code> property to <code>false</code> allows the resource to be removed on a subsequent update without affecting the attached bucket.

<i>Required</i>: No

<i>Type</i>: Boolean
</dd>

</dl>

## Attributes

<dl>

<dt><code>BucketName</code></dt>
<dd>

The <code>BucketName</code> attribute contains the name of the attached S3 bucket.

<i>Type</i>: String
</dd>

</dl>

## Examples

### Empty bucket when module is torn down

```yaml
- Resource: MyBucket
  Type: AWS::S3::Bucket

- Resource: EmptyMyBucket
  Type: LambdaSharp::S3::EmptyBucket
  Properties:
    Bucket: !Ref MyBucket
```
