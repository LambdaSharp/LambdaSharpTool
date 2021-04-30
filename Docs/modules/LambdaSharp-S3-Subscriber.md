---
title: LambdaSharp.S3.Subscriber - LambdaSharp Module
description: Documentation for LambdaSharp.S3.Subscriber module
keywords: module, s3, bucket, subscription, notification, documentation, overview
---

# Module: LambdaSharp.S3.Subscriber
_Version:_ [!include[LAMBDASHARP_VERSION](../version.txt)]

## Overview

The `LambdaSharp.S3.Subscriber` module defines the `LambdaSharp::S3::Subscription` resource type, which is automatically used by the LambdaSharp CLI to subscribe Lambda functions to S3 events.

## Resource Types
1. [LambdaSharp::S3::Subscription](LambdaSharp-S3-Subscription.md)

## Parameters

This module requires no parameters.

## Output Values

<dl>

<dt><code>ResourceHandlerRole</code></dt>
<dd>

The <code>ResourceHandlerRole</code> output contains the module IAM role ARN. This enables other modules to give additional permissions to the resource handler when required.

<i>Type:</i> AWS::IAM::Role

The following module sample shows how to import the `ResourceHandlerRole` output value and use it to attach additional permission to the S3 subscription handler.

```yaml
- Import: SubscriberRole
  Module: LambdaSharp.S3.Subscriber::ResourceHandlerRole

- Resource: S3SubscriberAccess
  Type: AWS::IAM::Policy
  Properties:
    PolicyName: !Sub "${AWS::StackName}S3BucketPolicy"
    PolicyDocument:
      Version: 2012-10-17
      Statement:
        - Sid: S3BucketPermissions
          Effect: Allow
          Action:
            - s3:GetBucketNotification
            - s3:PutBucketNotification
          Resource: !Ref MyBucket
    Roles:
      - !Ref SubscriberRole
```

</dd>

</dl>

