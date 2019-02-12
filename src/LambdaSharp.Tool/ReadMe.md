![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI

The λ# CLI is used to process the module definition, compile the C# projects, upload their packages to S3, generate a CloudFormation stack, and then create or update it. All operations are done in a single pass to facilitate greater productivity when building new λ# module. In addition, λ# uses a deterministic build process which enables it to skip updates when no code or configuration changes have occurred since the last deployment operation.

## Build, Publish, and Deploy Commands
1. [`build`](Docs/Tool-Build.md): build a module
1. [`publish`](Docs/Tool-Publish.md): publish a module
1. [`deploy`](Docs/Tool-Deploy.md): deploy a module

## Utility Commands
1. [`new module`](Docs/Tool-NewModule.md): create a new module
1. [`new function`](Docs/Tool-NewFunction.md): add a new function
1. [`new resource`](Docs/Tool-NewResource.md): add a new AWS resource
1. [`encrypt`](Docs/Tool-Encrypt.md): encrypt a value with a managed encryption key
1. [`list`](Docs/Tool-List.md): list deployed modules
1. [`info`](Docs/Tool-Info.md): show information about CLI setup

## Setup Commands
1. [`config`](Docs/Tool-Config.md): configure λ# CLI
1. [`init`](Docs/Tool-Init.md): initialize λ# deployment tier

## Utility Commands
1. [`util delete-orphan-lambda-logs`](Docs/Tool-UtilDeleteOrphanLambdaLogs.md): delete orphaned Lambda CloudWatch logs
1. [`util download-cloudformation-spec`](Docs/Tool-UtilDownloadCloudFormationSpec.md): download the CloudFormation types specification
