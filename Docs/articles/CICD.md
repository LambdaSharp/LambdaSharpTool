---
title: Continuous Integration/Continuous Delivery for LambdaSharp Modules
description: Tutorial on how to setup WSL with Amazon Linux 2
keywords: tutorial, wsl, linux, terminal
---

# CI/CD for LambdaSharp Modules

## Overview

The CI/CD pipeline made of two phases: _Build_ and _Deploy_. During the _Build_ phase, modules are built and published to an S3 bucket. Once all modules have been published, the _Deploy_ phase imports the published modules to the deployment tier and deploys them. The _Deploy_ phase can be run multiple times to deploy to the testing tier, then staging, and finally production. Since the _Deploy_ phase is using the same S3 bucket to import the modules, the exact same templates and artifacts are deployed to all deployment tiers. This process is both safer and more efficient.

## Build Phase

The _Build_ phase is responsible for building modules and publishing their CloudFormation templates and artifacts. Each _Build_ phase begins with the creation of a fresh S3 bucket. This ensures that each build is clean from previous builds. It also means that if a build is not completed successfully, it does not contaminate existing deployment tiers as their are isolated from the _Build_ phase.

> NOTE: By default, an AWS account has a limit of 100 S3 buckets. However, support can increase this limit to 1,000 S3 buckets.

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
# set the name of the AWS profile and the AWS region to use
PROFILE="AWS-PROFILE"
REGION="AWS-REGION"

# set commonly used LambdaSharp.Tool options
LASH_OPTIONS="--aws-profile ${PROFILE} --aws-region ${REGION} --no-ansi"

# create expiring bucket
lash new expiring-bucket ${LASH_OPTIONS} --expiration-in-days 3 ${BUILD_BUCKET}
```

**Output:**
```
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
# set AWS profile and region explicitly for all commands
AWS_OPTIONS="--profile ${PROFILE} --region ${REGION}"

# create an S3 bucket
aws ${AWS_OPTIONS} \
    s3api create-bucket \
    --acl private \
    --bucket ${BUILD_BUCKET}

# set the S3 bucket lifecycle to delete objects after 3 days
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
# name the build deployment tier
BUILD_TIER=BuildTier-${JITTER}

# create build deployment tier
lash init ${LASH_OPTIONS} \
    --tier ${BUILD_TIER} \
    --prompts-as-errors \
    --existing-s3-bucket-name ${BUILD_BUCKET} \
    --core-services Disabled \
    --skip-apigateway-check
```

**Output:**
```
LambdaSharp CLI (v0.8.0.7) - Create an S3 bucket that self-deletes after expiration
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              Bucket-build-20200728-i318a4 (User Initiated)
CREATE_IN_PROGRESS                  AWS::S3::Bucket                                         Bucket
CREATE_IN_PROGRESS                  AWS::IAM::Role                                          AutoDeleteFunctionRole
CREATE_IN_PROGRESS                  AWS::S3::Bucket                                         Bucket (Resource creation Initiated)
CREATE_IN_PROGRESS                  AWS::IAM::Role                                          AutoDeleteFunctionRole (Resource creation Initiated)
CREATE_COMPLETE                     AWS::IAM::Role                                          AutoDeleteFunctionRole
CREATE_IN_PROGRESS                  AWS::Lambda::Function                                   AutoDeleteFunction
CREATE_IN_PROGRESS                  AWS::Lambda::Function                                   AutoDeleteFunction (Resource creation Initiated)
CREATE_COMPLETE                     AWS::Lambda::Function                                   AutoDeleteFunction
CREATE_COMPLETE                     AWS::S3::Bucket                                         Bucket
CREATE_IN_PROGRESS                  AWS::Events::Rule                                       AutoDeleteTimer
CREATE_IN_PROGRESS                  AWS::Events::Rule                                       AutoDeleteTimer (Resource creation Initiated)
CREATE_COMPLETE                     AWS::Events::Rule                                       AutoDeleteTimer
CREATE_IN_PROGRESS                  AWS::Lambda::Permission                                 AutoDeleteTimerInvokePermission
CREATE_IN_PROGRESS                  AWS::Lambda::Permission                                 AutoDeleteTimerInvokePermission (Resource creation Initiated)
CREATE_COMPLETE                     AWS::Lambda::Permission                                 AutoDeleteTimerInvokePermission
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              Bucket-build-20200728-i318a4
=> Stack creation finished

=> S3 Bucket ARN: arn:aws:s3:::build-20200728-i318a4

Done (finished: 7/28/2020 10:41:24 AM; duration: 00:01:46.5435267)
```

### Publish modules to build tier

Each module must now be built and published to the build deployment tier. The `--module-origin` option overwrites the origin identifier to the specified value.

In addition, a build policy should specify which modules can be resolved during the build phase. The build policy ensures that no new dependencies are introduced by resolving module references only to specified versions. The following `build-policy.json` document specifies the modules and versions that are acceptable during the build phase. The build policy document is specified using the `--build-policy` command line option with a path to the JSON file.
```json
{
    "Modules": {
        "Allow": [
            "LambdaSharp.Core:0.8.0.6@lambdasharp",
            "LambdaSharp.S3.IO:0.8.0.6@lambdasharp"
        ]
    }
}
```

> NOTE: `lash publish` can build and publish multiple modules at once.

**Command:**
```bash
# set module origin explicitly
MODULE_ORIGIN="acme-corp"

# build and publish modules
lash publish ${LASH_OPTIONS} \
    --tier ${BUILD_TIER} \
    --prompts-as-errors \
    --module-origin ${MODULE_ORIGIN} \
    --build-policy build-policy.json \
    My.Module
```

**Output:**
```
LambdaSharp CLI (v0.8.0.7) - Publish LambdaSharp module

Reading module: My.Module\Module.yml
Compiling: My.Module (v1.0)
=> Building function MyFunction [netcoreapp3.1, Release]
=> Module compilation done: bin\cloudformation.json
Publishing module: My.Module
=> Uploading artifact: function_My.Module_MyFunction_F2FD08EF81DED1BB7309D59C5BC10415.zip
=> Uploading template: cloudformation_My.Module_952FDE40DB9F1C12A14BCFA77F1298B3.json
=> Published: My.Module:1.0@acme-corp

Done (finished: 7/28/2020 1:05:29 PM; duration: 00:00:09.4028395)
```

### Destroy build tier when all modules are published

Once all modules have been built and published, the build deployment tier is no longer needed and can be deleted.

**Command:**
```bash
lash nuke ${LASH_OPTIONS} \
    --tier ${BUILD_TIER} \
    --prompts-as-errors \
    --confirm-tier ${BUILD_TIER}
```

**Output:**
```
LambdaSharp CLI (v0.8.0.7) - Delete a LambdaSharp deployment tier
=> Inspecting deployment tier Build-i318a4
=> Found 1 CloudFormation stack to delete
=> Deleting Build-i318a4-LambdaSharp-Core
DELETE_COMPLETE    AWS::CloudFormation::Stack    Build-i318a4-LambdaSharp-Core
=> Stack delete finished

Done (finished: 7/28/2020 1:08:35 PM; duration: 00:00:04.7492584)
```

## Deploy Pipeline

### Upgrade deployment tier when needed

The recommended approach for production environments is to pin the expected deployment tier version. The `lash tier version` command can be used to check if the current deployment tier version is up-to-date. When it is not, the command exits with a non-zero status code. When that happens, the `lash init` command can be run to upgrade the deployment tier.

> NOTE: To upgrade the deployment tier unassisted, make sure the `DEPLOYMENT-TIER-PARAMETERS.YML` file contains all parameters required by the new deployment tier version.

```bash
# set the expected version for LambdaSharp.Core services
LASH_TIER_VERSION=0.8.0.2

# set the name of the deployment tier to use
DEPLOYMENT_TIER="prod"

# check if the current deployment tier version is up-to-date
if ! lash tier version --min-version ${LASH_TIER_VERSION}; then

    # update/upgrade deployment tier
    lash init ${LASH_OPTIONS} \
        --tier ${DEPLOYMENT_TIER} \
        --prompts-as-errors \
        --version ${LASH_TIER_VERSION} \
        --protect \
        --core-services enabled \
        --allow-upgrade \
        --parameters DEPLOYMENT-TIER-PARAMETERS.YML

    # (optional) import new versions of commonly used LambdaSharp modules
    lash publish  ${LASH_OPTIONS} \
        --tier ${DEPLOYMENT_TIER} \
        --prompts-as-errors \
        LambdaSharp.S3.IO:${LASH_TIER_VERSION}@lambdasharp \
        LambdaSharp.S3.Subscriber:${LASH_TIER_VERSION}@lambdasharp
fi
```

### Import module artifacts to deployment tier

With the deployment tier upgraded, the previously built modules can now be imported. The following command copies the CloudFormation templates and artifacts produced by the _Build_ phase to the deployment tier bucket. All module dependencies must be imported explicitly when using the `--from-origin` option.

> NOTE: `lash publish` can import multiple modules at once.

```bash
lash publish ${LASH_OPTIONS} \
    --tier ${DEPLOYMENT_TIER} \
    --prompts-as-errors \
    --from-origin ${BUILD_BUCKET} \
    My.Module@${MODULE_ORIGIN}
```

### Deploy imported modules to deployment tier

Finally, the newly built and imported modules can be deployed. The `--no-import` option makes sure dependencies must be resolved against modules published to the deployment tier bucket and cannot be imported from their origin. This guarantees only explicitly imported modules can be deployed.

```bash
lash deploy ${LASH_OPTIONS} \
    --tier ${DEPLOYMENT_TIER} \
    --prompts-as-errors \
    --no-import \
    --protect \
    --xray
    My.Module@${MODULE_ORIGIN} \
    --parameters MODULE_PARAMETERS.YML
```
