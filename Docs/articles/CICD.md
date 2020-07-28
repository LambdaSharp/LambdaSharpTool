---
title: Continuous Integration/Continuous Delivery for LambdaSharp Modules
description: Tutorial on how to setup WSL with Amazon Linux 2
keywords: tutorial, wsl, linux, terminal
---

# CI/CD for LambdaSharp Modules

## Overview

The CI/CD pipeline made of three phases: _Build_, _Validate_, and _Deploy_. During the _Build_ phase, modules are built and published to a new S3 bucket. Once all modules have been published, the _Validate_ phase deploys the modules from the S3 bucket to a staging deployment tier. Once the modules have passed their quality inspection, the same S3 bucket is used during the _Deploy_ phase to deploy the modules to the production deployment tier.

This process ensures that the exact same artifacts that were tested are the one being deployed, which is both more efficient and safer.

## Setup

```bash
PROFILE="AWS-PROFILE"
REGION="AWS-REGION"

LASH_OPTIONS="--aws-profile ${PROFILE} --aws-region ${REGION} --no-ansi"

MODULE_ORIGIN="module-origin"

BUILD_TIER=BuildTier-$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 8 | head -n 1)

DEPLOYMENT_TIER="production"
```

## Build Phase

The _Build_ phase is responsible for building modules and publishing their CloudFormation templates and artifacts. Each _Build_ phase begins with the creation of a fresh S3 bucket. This ensures that each build is clean from previous builds. It also means that if a build is not completed successful, it does not contaminate existing deployment tiers as their are isolated from the _Build_ phase.

> **NOTE:** By default, an AWS account has a limit of 100 S3 buckets. However, support can increase this limit to 1,000 S3 buckets.

### Create an S3 bucket name

The first step is to create a new S3 bucket name with the following constraints:
* Bucket names must be between 3 and 63 characters long. When using `lash new expiring-bucket`, the limit is 52 characters due to CloudFormation limits.
* Bucket names can consist only of lowercase letters, numbers, dots (.), and hyphens (-).
* Bucket names must begin and end with a letter or number.
* Bucket names must not be formatted as an IP address (for example, 192.168.5.4).
* Bucket names can't begin with xn-- (for buckets created after February 2020).
* Bucket names must be unique within a partition. A partition is a grouping of Regions. AWS currently has three partitions: aws (Standard Regions), aws-cn (China Regions), and aws-us-gov (AWS GovCloud [US] Regions).
* Buckets used with Amazon S3 Transfer Acceleration can't have dots (.) in their names. For more information about transfer acceleration, see Amazon S3 Transfer Acceleration.

It is recommended to generate the S3 bucket name from code versioning metadata, such as the Git SHA, branch name, or version number. In addition, the S3 bucket name should have a random suffix--or jitter--to guarantee uniqueness.

```bash
BUILD_BUCKET_PREFIX="build-20200728"
JITTER=$(cat /dev/urandom | tr -dc 'a-z0-9' | fold -w 6 | head -n 1)
BUILD_BUCKET=${BUILD_BUCKET_PREFIX}-${JITTER}
```

### Create a self-deleting S3 bucket to host the module artifacts

The `lash new expiring-bucket` command creates a new S3 bucket that deletes its contents in 3 days using a lifecycle policy. A Lambda function checks every 6 hours if the S3 bucket is empty. It it is, the Lambda function triggers a deletion of the CloudFormation stack.

This command makes it easy to create a temporary S3 bucket that cleans itself up, which is well-suited for the build process. The CloudFormation stack for the expiring S3 bucket takes just under 2 minutes to create.

**Command:**
```bash
lash new expiring-bucket ${LASH_OPTIONS} --expiration-in-days 3 ${BUILD_BUCKET}
```

**Output:**
```bash
LambdaSharp CLI (v0.8.0.7) - Create an S3 bucket that self-deletes after expiration
CREATE_COMPLETE    AWS::CloudFormation::Stack    Bucket-build-20200728-i318a4 (1m 40.23s)
CREATE_COMPLETE    AWS::IAM::Role                AutoDeleteFunctionRole (16.53s)
CREATE_COMPLETE    AWS::S3::Bucket               Bucket (22.83s)
CREATE_COMPLETE    AWS::Lambda::Function         AutoDeleteFunction (1.14s)
CREATE_COMPLETE    AWS::Events::Rule             AutoDeleteTimer (1m 1.03s)
CREATE_COMPLETE    AWS::Lambda::Permission       AutoDeleteTimerInvokePermission (10.38s)
=> Stack creation finished

=> S3 Bucket ARN: arn:aws:s3:::build-20200728-i318a4

Done (finished: 7/27/2020 10:23:54 PM; duration: 00:01:43.5610225)
```

Alternatively, the S3 bucket and  its lifecycle policy can be created using the AWS CLI. This approach is faster as it doesn't require a CloudFormation stack to be administered. However, it also requires an additional clean-up process to delete S3 buckets that are no longer needed.

**Command (alternative):**
```bash
AWS_OPTIONS="--profile ${PROFILE} --region ${REGION}"

aws ${AWS_OPTIONS} \
    s3api create-bucket \
    --acl private \
    --bucket ${BUILD_BUCKET}

aws ${AWS_OPTIONS} \
    s3api put-bucket-lifecycle-configuration \
    --bucket ${BUILD_BUCKET} \
    --lifecycle-configuration '{"Rules":[{"ID":"DeleteBuildArtifacts","Expiration":{"Days":3},"Filter":{"Prefix":""},"Status":"Enabled"}]}'
```

### Create a build tier using the new S3 bucket

The `lash init` command is used to create a new build deployment tier using the S3 bucket. _LambdaSharp Core Services_ are disabled as this deployment tier is only be used to publish the modules artifacts.

Similarly to the S3 bucket name, the build deployment tier name should be generated using a similar pattern. For convenience, the same jitter suffix is used to make it easier to relate a build deployment tier to its S3 bucket. The CloudFormation stack for the build deployment tier takes only seconds to create.

**Command:**
```bash
BUILD_TIER=BuildTier-${JITTER}

lash init ${LASH_OPTIONS} \
    --tier ${BUILD_TIER} \
    --prompts-as-errors \
    --existing-s3-bucket-name ${BUILD_BUCKET} \
    --core-services Disabled \
    --skip-apigateway-check
```

**Output:**
```bash
LambdaSharp CLI (v0.8.0.7) - Create or update a LambdaSharp deployment tier
Creating LambdaSharp tier 'Build-i318a4'
=> Stack creation initiated for Build-i318a4-LambdaSharp-Core
CREATE_COMPLETE    AWS::CloudFormation::Stack    Build-i318a4-LambdaSharp-Core (3.27s)
=> Stack creation finished

Done (finished: 7/27/2020 10:34:54 PM; duration: 00:00:08.5055239)
```

### Publish modules to build tier

```bash
lash publish ${LASH_OPTIONS} \
    --tier ${BUILD_TIER} \
    --prompts-as-errors \
    My.Module \
    --module-origin ${MODULE_ORIGIN}
```

### Destroy build tier when all modules are published

```bash
lash nuke ${LASH_OPTIONS} \
    --tier ${BUILD_TIER} \
    --prompts-as-errors \
    --confirm-tier ${BUILD_TIER}
```

## Testing Pipeline

> TODO:
> * Create a testing tier where the module can be validate in isolation
> * If all tests pass, proceed to the _Deploy Pipeline_

## Deploy Pipeline

### (optional) Upgrade deployment tier if needed

> TODO: steps to check if the deployment tier needs to be updated

```bash
lash init --version ${LASH_VERSION}
lash publish LambdaSharp.S3.IO:${LASH_VERSION}@lambdasharp
```

### Import module artifacts to deployment tier

**NOTE:** cannot import dependencies!

```bash
lash publish ${LASH_OPTIONS} \
    --tier ${DEPLOYMENT_TIER} \
    --prompts-as-errors \
    --from-origin ${BUILD_BUCKET} \
    My.Module@${MODULE_ORIGIN}
```

### Deploy imported modules to deployment tier

```bash
lash deploy ${LASH_OPTIONS} \
    --tier ${DEPLOYMENT_TIER} \
    --prompts-as-errors \
    --no-import \
    My.Module@${MODULE_ORIGIN} \
    -- parameters MODULE_PARAMETERS.yml
```
