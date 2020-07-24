---
title: Continuous Integration/Continuous Delivery for LambdaSharp Modules
description: Tutorial on how to setup WSL with Amazon Linux 2
keywords: tutorial, wsl, linux, terminal
---

# CI/CD for LambdaSharp Modules

## Environment variables

```bash
PROFILE="AWS-PROFILE"
REGION="AWS-REGION"

AWS_OPTIONS="--profile $PROFILE --region $REGION"
LASH_OPTIONS="--aws-profile $PROFILE --aws-region $REGION --prompts-as-errors --no-ansi"

MODULE_ORIGIN="module-origin"

DEPLOYMENT_TIER="deployment-tier"

BUILD_BUCKET_PREFIX="NAMING-CONVENTION"
BUILD_BUCKET_SUFFIX=$(cat /dev/urandom | tr -dc 'a-z0-9' | fold -w 8 | head -n 1)
BUILD_BUCKET=$BUILD_BUCKET_PREFIX-$BUCKET_SUFFIX

BUILD_TIER=BuildTier-$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 8 | head -n 1)

DEPLOYMENT_TIER="production"
```

## Build Pipeline

### Using LambdaSharp: Create a self-deleting S3 bucket to host the module artifacts

```bash
lash new expiring-bucket $LASH_OPTIONS $BUILD_BUCKET --expiration-in-days 3
```

### Using AWS CLI: Create a S3 bucket to host the module artifacts

```bash
aws $AWS_OPTIONS \
    s3api create-bucket \
    --acl private \
    --bucket $BUILD_BUCKET
```

Set the bucket lifecycle to automatically delete all artifacts after 30 days.
```bash
aws $AWS_OPTIONS \
    s3api put-bucket-lifecycle-configuration \
    --bucket $BUILD_BUCKET \
    --lifecycle-configuration '{"Rules":[{"ID":"DeleteBuildArtifacts","Expiration":{"Days":30},"Filter":{"Prefix":""},"Status":"Enabled"}]}'
```

**NOTE:** Bucket created with the AWS CLI must be manually deleted at a later date.

### Create a build tier using the new S3 bucket

```bash
lash init $LASH_OPTIONS \
    --tier $BUILD_TIER \
    --existing-s3-bucket-name $BUILD_BUCKET \
    --core-services Disabled \
    --skip-apigateway-check
```

### Publish modules to build tier

```bash
lash publish $LASH_OPTIONS \
    --tier $BUILD_TIER \
    My.Module \
    --module-origin $MODULE_ORIGIN
```

### Destroy build tier when all modules are published

```bash
lash nuke $LASH_OPTIONS \
    --tier $BUILD_TIER \
    --confirm-tier $BUILD_TIER
```

## Testing Pipeline

> TODO:
> * Create a testing tier where the module can be validate in isolation
> * If all tests pass, proceed to the _Deploy Pipeline_

## Deploy Pipeline

### Import module artifacts to deployment tier

```bash
lash publish $LASH_OPTIONS \
    --tier $DEPLOYMENT_TIER \
    --from-origin $BUILD_BUCKET \
    My.Module@$MODULE_ORIGIN
```

### Upgrade deployment tier if needed

> TODO: steps to check if the deployment tier needs to be updated

### Deploy imported modules to deployment tier

```bash
lash deploy $LASH_OPTIONS \
    --tier $DEPLOYMENT_TIER \
    --no-import \
    My.Module@$MODULE_ORIGIN
```
