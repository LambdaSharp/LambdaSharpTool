---
title: LambdaSharp::S3::Unzip - LambdaSharp.S3.IO Module
description: Documentation for LambdaSharp::S3::Unzip resource type
keywords: module, s3, io, documentation, resource, type, properties, attributes, unzip, package
---

# LambdaSharp::S3::Unzip

The `LambdaSharp::S3::Unzip` type creates a resource that unzips a [`Package` item](~/syntax/Module-Package.md) and copies its contents to an S3 bucket.

On creation, the contents of the source zip package are copied to the destination S3 bucket. On update, the resource checks which files in the zip package have been added, updated, or removed and only copies or deletes the affected files. On delete, the resource attempts to remove all files that were previously copied to the S3 bucket.

**NOTE:** The maximum size of the zip package is limited by the amount of temporary storage available to a Lambda function. At the time of this writing, this limit is 512MB.

## Using

```yaml
Using:

  - Module: LambdaSharp.S3.IO:0.5@lambdasharp
```

## Syntax

```yaml
Type: LambdaSharp::S3::Unzip
Properties:
  SourceBucket: String
  SourceKey: String
  DestinationBucket: String
  DestinationKey: String
```

## Properties

<dl>

<dt><code>DestinationBucket</code></dt>
<dd>

The <code>DestinationBucket</code> property specifies the destination S3 bucket name or ARN.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>DestinationKey</code></dt>
<dd>

The <code>DestinationKey</code> property specifies the destination prefix for all files copied from the zip package to destination S3 bucket.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>SourceBucket</code></dt>
<dd>

The <code>SourceBucket</code> property specifies the source S3 bucket name or ARN for the zip package.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>SourceKey</code></dt>
<dd>

The <code>SourceKey</code> property specifies the source key for the zip package.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>

## Attributes

<dl>

<dt><code>Url</code></dt>
<dd>

The <code>Url</code> attribute contains the S3 URL of the destination bucket and key: <code>s3://destination-bucket-name/destination-key-prefix</code>.

<i>Type</i>: String
</dd>

</dl>

## Examples

### Create package of web assets and deploy them to an S3 bucket

```yaml
- Resource: MyBucket
  Type: AWS::S3::Bucket

- Package: MyPackage
  Files: web-assets/

- Resource: DeployAssetsToBucket
  Type: LambdaSharp::S3::Unzip
  Properties:
    SourceBucket: !Ref DeploymentBucketName
    SourceKey: !Ref MyPackage
    DestinationBucket: !Ref MyBucket
    DestinationKey: assets/
```
