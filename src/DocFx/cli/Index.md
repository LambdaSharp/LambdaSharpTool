![λ#](~/images/CLI.png)

# LambdaSharp Command Line

The λ# CLI is used to process the module definition, compile the C# projects, upload their packages to S3, generate a CloudFormation stack, and then create or update it. All operations are done in a single pass to facilitate greater productivity when building new λ# module. In addition, λ# uses a deterministic build process which enables it to skip updates when no code or configuration changes have occurred since the last deployment operation.

## Build, Publish, and Deploy Commands
1. [`build`](Tool-Build.md): build a module
1. [`publish`](Tool-Publish.md): publish a module
1. [`deploy`](Tool-Deploy.md): deploy a module

## Development Commands
1. [`new module`](Tool-NewModule.md): create a new module
1. [`new function`](Tool-NewFunction.md): add a new function
1. [`new resource`](Tool-NewResource.md): add a new AWS resource
1. [`encrypt`](Tool-Encrypt.md): encrypt a value with a managed encryption key
1. [`list`](Tool-List.md): list deployed modules
1. [`info`](Tool-Info.md): show information about CLI setup

## Setup Commands
1. [`init`](Tool-Init.md): initialize λ# deployment tier

## Tier Commands
1. [`tier coreservices`](Tool-Tier-CoreServices.md): show/update λ# Core Services configuration

## Utility Commands
1. [`util create-invoke-methods-schema`](Tool-Util-CreateInvokeMethodsSchema.md): create JSON schema for compiled methods
1. [`util delete-orphan-logs`](Tool-Util-DeleteOrphanLogs.md): delete orphaned Lambda CloudWatch logs
1. [`util download-cloudformation-spec`](Tool-Util-DownloadCloudFormationSpec.md): download the CloudFormation types specification
