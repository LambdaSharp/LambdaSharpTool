---
title: LambdaSharp CLI New Command - Create an Expiring S3 Bucket
description: Create an S3 bucket that self-deletes after expiration
keywords: cli, cloudformation, public, sharing, s3, bucket, module
---
# Create Expiring S3 Bucket

The `new expiring-bucket` command is used to create a private S3 bucket that self deletes after it expires.

## Arguments

The `new expiring-bucket` command takes a single argument that specifies the S3 bucket name.

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
lash new expiring-bucket my-lambdasharp-bucket
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

<dt><code>--expiration-in-days &lt;VALUE&gt;</code></dt>
<dd>

(optional) Number of days until the bucket expires and is deleted (default: 7 days). Minimum value is 1 day. Maximum value is 365 days.
</dd>

</dl>

## Examples

### Create an S3 bucket that self-deletes in 3 days

__Using PowerShell/Bash:__
```bash
lash new expiring-bucket my-bucket --expiration-in-days 3
```

Output:
```
LambdaSharp CLI (v0.8.0.7) - Create an S3 bucket that self-deletes after expiration
CREATE_COMPLETE    AWS::CloudFormation::Stack    Bucket-my-bucket (51.09s)
CREATE_COMPLETE    AWS::IAM::Role                AutoDeleteFunctionRole (19.34s)
CREATE_COMPLETE    AWS::S3::Bucket               Bucket (23.66s)
CREATE_COMPLETE    AWS::Lambda::Function         AutoDeleteFunction (1.06s)
CREATE_COMPLETE    AWS::IAM::Policy              AutoDeleteFunctionSelfDeletePolicy (18.28s)
=> Stack creation finished

=> S3 Bucket ARN: arn:aws:s3:::my-bucket

Done (finished: 7/23/2020 1:05:50 PM; duration: 00:00:53.7584203)
```
