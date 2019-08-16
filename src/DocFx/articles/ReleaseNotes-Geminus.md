# λ# - Geminus (v0.7) - 2019-06-26

> Geminus of Rhodes, was a Greek astronomer and mathematician, who flourished in the 1st century BC. An astronomy work of his, the Introduction to the Phenomena, still survives; it was intended as an introductory astronomy book for students. He also wrote a work on mathematics, of which only fragments quoted by later authors survive. [(Wikipedia)](https://en.wikipedia.org/wiki/Geminus)

## What's New

This release focuses on making it easy to share modules with others and streamlining the creation and upgrading of λ# deployment tiers. Prior to this release, λ# modules had to be made available in an S3 bucket for each region used by a deployment tier. In 0.7.0, the λ# CLI now automatically imports modules and their assets into the S3 bucket of the deployment tier, regardless of the origin region. This new behavior also safeguards against disruption should the original λ# module become unavailable at a later date since a copy is maintained in the deployment bucket. To further enhance deployment consistency, external modules are only imported during the publishing phase. The deployment phase relies entirely on the modules available in the S3 bucket of the deployment tier.


## BREAKING CHANGES

### λ# Module

* The `Namespace:` declaration has been renamed to [`Group:`](~/syntax/Module-Group.md).
* Module references in `Using:` and `Nested:` declarations now require an origin suffix. For example, `LambdaSharp.S3.IO:0.5` must now be written as `LambdaSharp.S3.IO:0.5@lambdasharp`.

### λ# CLI

* The `lash config` command was integrated into the `lash init` command to streamline the setup process.
* The λ# CLI will now fail to publish a stable version of a module when the `git-sha` is prefixed with `DIRTY-`, which indicate uncommitted local changes.  This behavior can be overwritten with `--force-publish`.

### λ# Assemblies

* `ALambdaFunction.InfoStruct.ModuleOwner` has been renamed to `ALambdaFunction.InfoStruct.ModuleNamespace`
* `ALambdaFunction.DefaultSecretKeyId` has been removed


## New λ# Module Features


## New λ# CLI Features

### Publish Command

The `lash publish` command has a new option to override the module origin information using `--module-origin`. By default, the module origin for a newly published module is the name of the deployment bucket. However, it may be sometimes necessary to publish a new version of a module that originated from somewhere else, such as deploying an urgent fix. With the `--module-origin` option, it is possible to publish a module into the S3 bucket of the deployment tier, while making it look like it was imported. As a result, all subsequent deployments will resolve their dependencies to this new module. The imported module and its assets are annotated with a metadata field (`x-amz-meta-lambdasharp-origin`) that describes their true origin.

The `lash publish` command is responsible for uploading the module assets to the deployment bucket, as well as importing all dependencies. Modules from a foreign origin can also be imported explicitly by this command.

__Using PowerShell/Bash:__
```bash
lash publish LambdaSharp.S3.Subscriber:0.7.0@lambdasharp
```

The following text should appear (or similar):
```
LambdaSharp CLI (v0.7.0) - Publish LambdaSharp module
=> Imported LambdaSharp.S3.Subscriber:0.7.0@lambdasharp

Done (finished: 8/16/2019 10:27:01 AM; duration: 00:00:04.5205374)
```

To accommodate combining modules from various origins in a single S3 deployment bucket, the structure of the S3 keys had to be revisited. All published/imported modules and their assets now have the following prefix `{Module::Origin}/{Module::Namespace}/{Module::Name}`, which ensures there will never be any conflicts, since S3 bucket names (i.e. the module origin) is guaranteed to be globally unique. The module manifest--which describes the parameters, resource type definitions, dependencies, and assets--is located at `{Module::Origin}/{Module::Namespace}/{Module::Name}/{Module::Version}`. The module assets, across all versions, are located at `{Origin}/{ModulePrefix}/{ModuleSuffix}/.assets/`.


### New Function Command

The `lash new function` command has been enhanced with types for C# functions. It can now create the scaffolding for functions to handle API Gateway, custom resources, SQS queues, scheduled CloudWatch events, SNS topics, WebSocket, and generic requests by using the `--type` option. If no type is provided, the λ# CLI will prompt for one. In addition, the `--memory` and `--timeout` options have been added. When omitted, they default to 256 (MB) and 30 (seconds), respectively.

### New Bucket Command

The `lash new bucket` command is used to create a new public S3 bucket configured to require requestors to pay for data transfer. This is the recommended configuration when publicly sharing λ# modules so the owner of the bucket only pays for the storage and not its access, which could become expensive for a popular module. Once the bucket is created, it can be used with a deployment tier to enable publishing to it.

__Using PowerShell/Bash:__
```bash
lash new bucket my-lambdasharp-bucket
```

The following text should appear (or similar):
```
LambdaSharp CLI (v0.7.0) - Create new public S3 bucket for sharing LambdaSharp modules
CREATE_COMPLETE    AWS::CloudFormation::Stack    PublicLambdaSharpBucket-my-lambdasharp-bucket
CREATE_COMPLETE    AWS::S3::Bucket               Bucket
CREATE_COMPLETE    AWS::S3::BucketPolicy         BucketPolicy
=> Stack creation finished
=> Updating S3 Bucket for Requester Pays access

Done (finished: 8/16/2019 10:20:18 AM; duration: 00:00:32.5327433)
```


## New λ# Assembly Features

* Added [`ALambdaCustomResourceFunction.Abort(string)`](xref:LambdaSharp.CustomResource.ALambdaCustomResourceFunction.Abort(string)) method to abort the creation or update of a custom resource. `Abort()` will cause CloudFormation to respond with a failure code and showing the provided message.
* Added [`LambdaSharp.CustomResource.Request<TProperties>.StackId`](xref:LambdaSharp.CustomResource.Request`1.StackId) property to custom resource request to uniquely identify the CloudFormation stack from which the request originated.
* The `ALambdaFinalizerFunction` class now checks confirms the CloudFormation stack is being deleted before triggering the [DeleteDeployment(FinalizerProperties)] method. This change allows a Finalizer to be removed from a module without triggering its delete logic.

---

## Removed Module::DefaultSecretKey

## Minimal Deployment Tier

* `lash config` is gone
* default secret key is gone
* ability to create a deployment tier without core services

## Updated Rollbar messages
* `Task timed out after 15.02 seconds` vs `Lambda timed out after 15.02 seconds`
* `Process exited before completing request` vs `Lambda exited before completing request`
* `Process ran out of memory (Max: 128 MB)` vs `Lambda ran out of memory (Max: 128 MB)`
* `Process nearing execution limits (Memory 80.12 %, Duration: 91.23 %)` vs `Lambda nearing execution limits (Memory 80.12 %, Duration: 91.23 %)`

## New format for LambdaSharp dependencies in .csproj files

* Making it contributor friendly

## ALambdaCustomResourceFunction

* added `Abort()` method

## X-Ray

* can be enabled for root module or all modules
* added support for api gateway

## lash init

* `--quick-start` option
* `--cli-profile`, `--module-bucket-names`, `--deployment-notifications-topic` are obsolete
* tier name can now be empty (Q: is that the default?)

## lash encrypt

* new `--decrypt` option

## lash info

* show how much lambda storage is used
* show how much lambda reserved capacity is used

## lash build options

* `--module-version`: (optional) Override the module version
* `--module-build-date`: (optional) Override module build date [yyyyMMddHHmmss]

## last new function

* `--use-project-reference` and `--use-nuget-reference` are obsolete

## ALambdaFinalizerFunction

* finalizer now makes sure that a stack is being deleted before returning a different physical id for itself
* physical id
  * before: `Finalizer:{request.OldResourceProperties.DeploymentChecksum}`
  * after: `Finalizer:Module`

## Misc

* `lash tier coreservices --enabled` was `--enable` before

## Upgrade Procedure

* ensure that λ# CLI v0.6.0.3 is installed
* ensure that the deployment tier and all deployed modules have been upgraded to v0.6.0.3
* run `lash tier coreservices --disable` to disable LambdaSharp.Core services foo all deployed modules
* install λ# CLI v0.7.0
* run `lash init` to upgrade the deployment tier
* run `lash tier coreservices --enabled` to enable LambdaSharp.Core services
