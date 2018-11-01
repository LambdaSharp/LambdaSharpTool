# λ# - Damo (v0.4-RC1) - 2018-11-01

> Damo was a Pythagorean philosopher said by many to have been the daughter of Pythagoras and Theano. [(Wikipedia)](https://en.wikipedia.org/wiki/Damo_(philosopher))

## What's New

The objective of the λ# 0.4 _Damo_ release has been to enable λ# modules to be deployed to different tiers without requiring the module, or underlying code, to be rebuilt each time. To achieve this objective, all compile-time operations have been translated into equivalent CloudFormation operations that can be resolved at stack creation time. This change required adding support for [CloudFormation parameters](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/parameters-section-structure.html), which means that λ# modules can now be parameterized. For added convenience, λ# generates a [CloudFormation interface](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-resource-cloudformation-interface.html), so that module parameters are laid out in a logical manner in the AWS Console.

In addition, λ# 0.4 _Damo_ introduces a new module composition model that makes it easy to deploy modules that build on each other. A design challenge was to make this new composition model both easy to use while maintaining flexibility in how module dependencies are resolved. The solution leverages [CloudFormation exports](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-stack-exports.html) which provide built-in tracking of dependencies and termination protection. λ# cross-module references are automatically resolved by default, but can be overwritten when needed to reference other modules or use existing resources instead. The default behavior preserves the ease-of-use, while the flexibility of referencing different modules enables the freedom of choosing a preferred deployment topology. Finally, by allowing explicit resources names or values to be passed in, it is possible to deploy modules in legacy environments where resources were created through a different process.

Finally, λ# 0.4 _Damo_ introduces a new core service--the `λ# Registrar`--that is responsible for registering modules and processing the CloudWatch Logs of their deployed Lambda functions. By monitoring the logs, `λ# Registrar` can automatically detect and report out-of-memory and timeout failures. As an extra bonus, `λ# Registrar` can optionally be integrated with [Rollbar](http://rollbar.com) to create projects per module to track their warnings and errors.



## BREAKING CHANGES

The following change may impact modules created with previous releases.

### Module Definition

With the addition of new sections to the module definition, and with an eye towards the future, some attributes/sections have been renamed for consistency.
* The module name is now specified with the `Module` attribute (previously `Name`).
* The function name is now specified with the `Function` attribute (previously `Name`).
* Variables--formerly parameters--are now specified in the `Variables` section (previously `Parameters`). Similarly, for nested variables. The previous name was causing confusion with CloudFormation terminology.
* Variables are no longer automatically added to function environments. Instead, the scope of a variable is controlled by the `Scope` attribute. The old behavior caused too many cases of circular dependencies, which were tedious to diagnose.
* The variable name is now specified with the `Var` attribute (previously `Name`).
* The `Export` attribute is no longer supported for variables and functions, because the AWS Parameter Store is no longer used for cross-module references.
* The `Import` attribute is no longer supported for variables, because the AWS Parameter Store is no longer used for cross-module references.
* The `Values` attribute has been removed since the `Value` attribute can now accept arbitrary expressions.
* A package resource is now specified with the `Package` attribute, followed by its variable name.
* The macro function source was replaced in favor of a macro definition in the `Outputs` section.

### λ# Tool

* The λ# Tool has been renamed to λ# CLI.
* Function folders no longer need to be prefixed with the module name. They will still be found for backwards compatibility, but the new recommended naming is to just use the function name as the folder name.
* With the introduction of `--cli-profile` there was the need to rename `--profile` to `--aws-profile` to avoid ambiguity.
* The role of the pre-processor has been greatly reduced to make generated CloudFormation templates more portable. As such, support for variable substitutions using the moustache (`{{ }}`) notation has been removed. Conditional inclusion is still supported, but since it is done at the pre-processor level, the benefits are no longer available after the build phase is complete. To select which conditional inclusions to keep, use the new `--selector` option with the `build` command. For convenience, the `deploy` command defaults to using the `--tier` option value as selector, if no selector is provided. This makes running the `deploy` command very similar to what it was in the past.

### λ# Assemblies

* CloudFormation resources are now always resolved to the ARN of the created resource. Previously, created resources were resolved using the `!Ref` operation, which varies by resource type. This change was necessary to homogenize CloudFormation resources with resource references found in module parameters and cross-module references. For convenience, the `AwsConverters` class contains methods for extracting resource names from S3/SQS/DynamoDB/etc. ARNs.
* The `ALambdaFunction<TRequest>` base class was removed in favor of `ALambdaFunction<TRequest, TResponse>`.



## New λ# CLI Features

### Setup

The λ# CLI is now a global dotnet tool, which makes it trivial to install. No more need to check-out the [LambdaSharpTool GitHub repository](https://github.com/LambdaSharp/LambdaSharpTool) for creating modules unless to contribute to it.
```bash
dotnet tool install -g MindTouch.LambdaSharp.Tool --version 0.4-WIP
```

As part of the λ# CLI setup procedure, the CLI must be configured for the AWS account. The configuration step creates a profile and resources required to deploy λ# modules. The profile information is stored in the AWS Parameter Store so that it can be shared with team members. Multiple CLI profiles can be configured when needed.
```bash
dotnet lash config
```

Initializing a deployment tier has been streamlined into a single command, which deploys the required bootstrap modules from a public λ# bucket. Alternatively, for λ# contributors, the CLI can deploy a locally compiled version of the bootstrap modules.
```bash
dotnet lash init --tier Demo
```

For complete instructions and options, check out the updated [setup documentation](../Bootstrap/).

### Build, Publish, and Deploy

The λ# deployment process has been broken down into three commands: `build`, `publish`, and `deploy`.

#### Build Command

The `build` command compiles the λ# module definition into a CloudFormation template, the functions into Lambda-ready packages, and compresses the file packages. The `build` command does not upload any assets and, therefore, does not require a λ# CLI profile, deployment tier, or even AWS account to work.

For .Net Core, before compiling the function projects, the λ# CLI verifies that the correct λ# assemblies are referenced. This ensures that λ# CLI produced CloudFormation template is compatible with the Lambda functions when they get deployed. In addition `dotnet restore` is run to ensure the latest dependencies are used.

To build the `Module.yml` file in the current folder:
```bash
dotnet lash build
```

To build the `Module.yml` file in another folder:
```bash
dotnet lash MyCompany/MyModule
```

#### Publish Command

The `publish` command takes a path to a module manifest file as argument (e.g. `MyModule/bin/manifest.json`). The manifest is used to identify the module assets that must be uploaded to the deployment bucket associated with the λ# CLI profile. The `publish` command skips assets that have been previously uploaded and haven't changed.

If the `publish` command is used without referencing a module manifest file, it invokes the `build` command first.

To publish a previously built module:
```bash
dotnet lash publish MyCompany/MyModule/bin/manifest.json
```

#### Deploy Command

The `deploy` command takes a module name and version number as argument (e.g. `MyModule:1.0`) and attempts to locate a published module with the requested version number. The `deploy` command selects the latest compatible version for deployment. If the version is omitted or specified as `*`, the `deploy` command will use the latest version it can find.

If the `deploy` command is used with an argument that resolves a module file, it invokes the `build` and `publish` commands first. Similarly, if the argument resolves to a manifest file, the `deploy` command invokes the `publish` command first.

To deploy a published module:
```bash
dotnet lash deploy MyModule:1.0
```

To deploy a published module under an alternative name:
```bash
dotnet lash deploy --name MySpecialModule MyModule:1.0
```

To build, publish, and deploy the `Module.yml` file in the current folder:
```bash
dotnet lash deploy
```

To build, publish, and deploy the `Module.yml` file in another folder:
```bash
dotnet lash deploy MyCompany/MyModule
```

##### Module Parameters Processing

The λ# CLI can be invoked with an optional inputs file to supply the module parameter values for the deployment.
```bash
dotnet lash deploy --inputs inputs.yml MyCompany/MyModule
```

The module parameters file is processed by the λ# CLI before being applied as follows:
* List of values are converted into a comma-separated text value.
* The `Secrets` attribute is processed to resolve KMS key aliases into key ARNs for the deployment AWS account/region.

For example, this parameters file:
```yaml
MyFirstParameter: 123
MySecondParameter:
  - abc
  - xyz
Secrets:
  - alias/MyDeveloperKey
  - alias/MyOtherKey
```

Is converted into these actual module parameter values:
```yaml
MyFirstParameter: 123
MySecondParameter: abc,xyz
Secrets: arn:aws:kms:us-east-1:123456789012:key/1234abcd-12ab-34cd-56ef-1234567890ab,arn:aws:kms:us-east-1:123456789012:key/abcd1234-12ab-34cd-56ef-1234567890ab
```

##### Pre-deployment Check

The λ# CLI goes through a pre-deployment check before updating an existing CloudFormation stack:
1. The module to be deploying and the deployed module names must match.
1. The module to be deployed must have the same or newer version than the deployed module.

The pre-deployment check can be skipped with the `--force-deploy` option.


### Misc

#### λ# Info

The `info` command was enhanced to show information about other installed tools that λ# CLI depends on, such as `dotnet` and `git`. In addition, sensitive information--like the AWS account ID--are hidden unless `--show-sensitive` option is used.

See the [updated documentation](../src/MindTouch.LambdaSharp.Tool/Docs/Tool-Info.md) for more details.

#### λ# Encrypt

The `encrypt` command was added to make it easier to encrypt sensitive information. The command can either use a specific KMS key or use the default KMS key for the deployment tier.

See the [updated documentation](../src/MindTouch.LambdaSharp.Tool/Docs/Tool-Encrypt.md) for more details.

### λ# Encrypt

The `new function` command now allows specifying the target language when adding a function.

See the [updated documentation](../src/MindTouch.LambdaSharp.Tool/Docs/Tool-NewFunction.md) for more details.





> TODO: continue here

## New λ# Module Features

### Default Variables

* `Module::Id`
* `Module::Name`
* `Module::Version`
* `Module::DeadLetterQueueArn`
* `Module::LoggingStreamArn`
* `Module::DefaultSecretKeyArn`
* `Variables`
    * add `Scope:` attribute to variables (can either be `*` or a list of function names)
* substitute parameter values in `!Ref` and `!Sub` operations
    * find and replace `!Ref` parameter references in-place rather than to letting cloudformation replace them for us

### Module Inputs

* allow `DependsOn: Foo`
* `Inputs` section
    * `Section` and `Label` attribute
    * `Input`
    * `Import`
        * use `ConstraintDescription` & `AllowedPattern`
        * use cloudformation input pattern validation for `!Import:` defaults
    * add `Scope:` attribute to input parameters (can either be `Module` or `Function`)
    * conditional inputs
        * conditional resources
        * auto-create resource if an input is not provided
            ```yaml
            Inputs:
                - Name: Foo
                Default: ""
                Resource:
                    Type: ABC
                    Properties:
                        XYZ: 123
            ```
    * `Type: Secret` for input/import

### Module Outputs
* `Outputs` section
    * `Output`
    * `CustomResource`
        ```yaml
        - CustomResource: LambdaSharp::S3PackageLoader
            Handler: CustomResourceTopic
            Description: SNS Topic ARN for subscribing to S3 buckets
        ```
    * `Output:` without a `Value:` attribute should check if there is a parameter/variable with the same name and export it using `!Ref`; also reuse `Description:` attribute is none is specified
    * Export pattern:
        * shared resource: `${Tier}-${ModuleId}::{VariableName}`
        * custom resource: `${Tier}-CustomResource-${CustomResourceName}`
        * use CloudFormation stack exports for custom resources
            * benefit is that it will track which stacks are currently using it

### Misc

* allow `!Ref` for alexa skill
* ability to specify the attribute name to obtain the ARN in cloudformation resources

## New λ# Deployment Tier Features

* module registration (similar to what we did with rollbar)
* configurable (LambdaSharp Module)
    * `LoggingStreamRetentionPeriod`
    * `DefaultSecretKeyRotationEnabled`
* CloudWatch Logs
    * clean-up on function deletion
    * log retention limit

## New λ# Assembly Features

* `ALambdaFunction` now has `DecryptSecretAsync()` and `EncryptSecretAsync()` methods (uses DefaultSecretKey by default)
* `string DecryptSecretAsync(string secret, Dictionary<string, string> encryptionContext = null)`
* `string EncryptSecretAsync(string text, string encryptionKeyId = null, Dictionary<string, string> encryptionContext = null)`
* `T T DeserializeJson<T>(Stream stream)`
* `T DeserializeJson<T>(string json)`
* `string SerializeJson(object value)`


## Fixes

## Internal Changes

* change `${Tier}-` to `${DeploymentPrefix}`
* need to create module `manifest.json` file that contains the version, module name, and dependencies
* always add `ModuleVersion` to output values
* `cloudformation-v{version}-{md5}.json`
* there is now the notion of a module ID, which is the instance name of the module
