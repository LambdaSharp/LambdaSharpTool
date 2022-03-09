---
title: LambdaSharp CLI New Command - Create a Public S3 Bucket
description: Create a public S3 bucket with Requester Pays access
keywords: cli, cloudformation, public, sharing, s3, bucket, module
---
# Create Public S3 Bucket

The `new public-bucket` command is used to create a public S3 bucket. The bucket is configured to be publicly accessible, but requires the [requester to pay](https://docs.aws.amazon.com/AmazonS3/latest/dev/RequesterPaysBuckets.html) for all data transfer. This ensures that the owner of the S3 bucket only pays for the storage of shared LambdaSharp modules.

## Arguments

The `new public-bucket` command takes a single argument that specifies the S3 bucket name.

The following are the rules for naming S3 buckets in all AWS Regions:
* Bucket names must be unique across all existing bucket names in Amazon S3.
* Bucket names must comply with DNS naming conventions.
* Bucket names must be at least 3 and no more than 63 characters long.
* Bucket names must not contain uppercase characters or underscores.
* Bucket names must start with a lowercase letter or number.
* Bucket names must be a series of one or more labels. Adjacent labels are separated by a single period (.). Bucket names can contain lowercase letters, numbers, and hyphens. Each label must start and end with a lowercase letter or a number.
* Bucket names must not be formatted as an IP address (for example, 192.168.5.4).
* When you use virtual hosted–style buckets with Secure Sockets Layer (SSL), the SSL wildcard certificate only matches buckets that don't contain periods. To work around this, use HTTP or write your own certificate verification logic. We recommend that you do not use periods (".") in bucket names when using virtual hosted–style buckets.

```bash
lash new public-bucket my-lambdasharp-bucket
```

## Options

<dl>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS profile from the AWS credentials file
</dd>

<dt><code>--aws-region &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS region (default: read from AWS profile)
</dd>

</dl>

## Examples

### Create a public S3 bucket with Requester Pays access

__Command Line:__
```bash
lash new public-bucket my-lambdasharp-bucket4
```

Output:
```
LambdaSharp CLI (v0.7.0) - Create new public S3 bucket with Requester Pays access
CREATE_COMPLETE    AWS::CloudFormation::Stack    Bucket-my-lambdasharp-bucket4
CREATE_COMPLETE    AWS::S3::Bucket               Bucket
CREATE_COMPLETE    AWS::S3::BucketPolicy         BucketPolicy
=> Stack creation finished
=> Updating S3 Bucket for Requester Pays access

=> S3 Bucket ARN: arn:aws:s3:::my-lambdasharp-bucket

Done (finished: 9/7/2019 8:02:19 PM; duration: 00:00:32.2956094)
```
