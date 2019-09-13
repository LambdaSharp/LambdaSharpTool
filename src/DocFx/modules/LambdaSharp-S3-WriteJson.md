---
title: LambdaSharp::S3::WriteJson - LambdaSharp.S3.IO Module
description: Documentation for LambdaSharp::S3::WriteJson resource type
keywords: module, s3, io, documentation, resource, type, properties, attributes, write, json
---

# LambdaSharp::S3::WriteJson

The `LambdaSharp::S3::WriteJson` type creates a resource that writes a JSON file to an S3 bucket.

On creation and update, the <code>Contents</code> property is serialized into a JSON document and written to the S3 bucket. On deletion, the JSON document is deleted from the S3 bucket.

## Using

```yaml
Using:

  - Module: LambdaSharp.S3.IO:0.5@lambdasharp
```

## Syntax

```yaml
Type: LambdaSharp::S3::WriteJson
Properties:
  Bucket: String
  Key: String
  Contents: JSON
```

## Properties

<dl>

<dt><code>Bucket</code></dt>
<dd>

The <code>Bucket</code> property specifies the destination S3 bucket name or ARN.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Contents</code></dt>
<dd>

The <code>Contents</code> property specifies the contents of the JSON document to be written.

<i>Required</i>: Yes

<i>Type</i>: Boolean
</dd>

<dt><code>Key</code></dt>
<dd>

The <code>Key</code> property specifies the destination key where the JSON file is written to.

<i>Required</i>: Yes

<i>Type</i>: Boolean
</dd>

</dl>

## Attributes

<dl>

<dt><code>Url</code></dt>
<dd>

The <code>Url</code> attribute contains the S3 URL of the bucket and key: <code>s3://bucket-name/key</code>.

<i>Type</i>: String
</dd>

</dl>

## Examples

### Write a `config.json` file that contains the API Gateway URL

```yaml
- Resource: MyBucket
  Type: AWS::S3::Bucket

- Resource: WriteConfigJson
  Type: LambdaSharp::S3::WriteJson
  Properties:
    Bucket: !Ref MyBucket
    Key: config.json
    Contents:
      Api: !Ref Module::RestApi::Url
```
