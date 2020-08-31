---
title: LambdaSharp CLI Commands
description: List of commands in LambdaSharp CLI
keywords: cli, command, overview
---
![λ#](~/images/CLI.png)

# LambdaSharp Command Line

The LambdaSharp CLI is used to process the module definition, compile the C# projects, upload their packages to S3, generate a CloudFormation stack, and then create or update it. All operations are done in a single pass to facilitate greater productivity when building new LambdaSharp module. In addition, LambdaSharp uses a deterministic build process which enables it to skip updates when no code or configuration changes have occurred since the last deployment operation.

## Build, Publish, and Deploy Commands
1. [`build`](Tool-Build.md): build a module
1. [`publish`](Tool-Publish.md): publish a module
1. [`deploy`](Tool-Deploy.md): deploy a module

## Development Commands
1. [`new module`](Tool-New-Module.md): create a new module
1. [`new function`](Tool-New-Function.md): add a new function to a module
1. [`new resource`](Tool-New-Resource.md): add a new AWS resource to a module
1. [`new public-bucket`](Tool-New-PublicBucket.md): create a public S3 bucket with Requester Pays access
1. [`new expiring-bucket`](Tool-New-ExpiringBucket.md): create an expiring S3 bucket that self-deletes after expiration
1. [`encrypt`](Tool-Encrypt.md): encrypt a value with a managed encryption key
1. [`list`](Tool-List.md): list deployed modules
1. [`info`](Tool-Info.md): show information about CLI setup

## Setup Commands
1. [`init`](Tool-Init.md): initialize LambdaSharp deployment tier

## Tier Commands
1. [`tier coreservices`](Tool-Tier-CoreServices.md): show/update LambdaSharp Core Services configuration
1. [`tier list`](Tool-Tier-List.md): list all deployment tiers
1. [`tier version`](Tool-Tier-Version.md): show the version of the deployment tier

## Utility Commands
1. [`util create-invoke-methods-schema`](Tool-Util-CreateInvokeMethodsSchema.md): create JSON schema for compiled methods
1. [`util delete-orphan-logs`](Tool-Util-DeleteOrphanLogs.md): delete orphaned Lambda CloudWatch logs
1. [`util download-cloudformation-spec`](Tool-Util-DownloadCloudFormationSpec.md): download the CloudFormation types specification
1. [`util list-lambdas`](Tool-Util-ListLambdas.md): list Lambda functions by CloudFormation stack
1. [`util list-modules`](Tool-Util-ListModules.md): list published LambdaSharp modules at an origin
1. [`util show-kinesis-failed-logs`](Tool-Util-ShowKinesisFailedLogs.md): show the failed Kinesis Firehose records from the S3 logging bucket
1. [`util show-parameters`](Tool-Util-ShowParameters.md):
