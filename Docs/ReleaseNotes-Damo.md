# λ# - Damo (v0.4) - 2018-10-17

> Damo was a Pythagorean philosopher said by many to have been the daughter of Pythagoras and Theano. [(Wikipedia)](https://en.wikipedia.org/wiki/Damo_(philosopher))

## What's New

The objective of the λ# 0.4 _Damo_ release has been to enable λ# modules to be deployed to different tiers without requiring the module, or underlying code, to be rebuilt each time. To achieve this objective, all compile-time operations have been translated into equivalent CloudFormation operations that can be resolved at stack creation time. This change required adding support for [CloudFormation parameters](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/parameters-section-structure.html), which means that λ# modules can now be parameterized. For added convenience, λ# generates a [CloudFormation interface](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-resource-cloudformation-interface.html), so that parameters can be laid out in a logical manner.

In addition, λ# 0.4 _Damo_ introduces a new module composition model that makes it easy to deploy modules that build on each other. A design challenge was to make this new composition model both easy to use while maintaining flexibility in how module dependencies are resolved. The solution leverages [CloudFormation exports](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-stack-exports.html) which provide built-in tracking of dependencies and termination protection.

Finally, λ# 0.4 _Damo_ introduces a new core service--the `LambdaSharpRegistrar`--that is responsible for registering all modules and processing the CloudWatch Logs of their deployed Lambda functions. By monitoring the logs, `LambdaSharpRegistrar` can detect out-of-memory and timeout failures, which were not reported on before. As an extra bonus, `LambdaSharpRegistrar` can optionally be integrated with [Rollbar](http://rollbar.com) to create projects and manage errors.

## BREAKING CHANGES

The following change may impact modules you have created using previous releases.

### Pre-processor

The role of the pre-processor has been greatly reduced to make generated CloudFormation templates more portable. As such, support for variable substitutions using the moustache (`{{ }}`) notation has been removed. Conditional inclusion is still supported, but since it is done at the pre-processor level, the benefits are no longer available after the build phase is complete. To select which conditional inclusions to keep, use the new `--selector` option with the `build` command. For convenience, the `deploy` command defaults to using the `--tier` option value as selector, if no selector is provided. This makes running the `deploy` command very similar to what it was in the past.

### Module File

With the addition of new sections to the module file, and with an eye towards the future, some attributes/sections have been renamed for consistency.
* The module name is now specified with the `Module` attribute (previously `Name`).
* The function name is now specified with the `Function` attribute (previously `Name`).
* Variables--formerly parameters--are now specified in the `Variables` section (previously `Parameters`). Similarly, for nested variables. The previous name was causing confusion with CloudFormation terminology.
* The variable name is now specified with the `Var` attribute (previously `Name`).
* The `Export` attribute is no longer supported for variables and functions, because the AWS Parameter Store is no longer used for cross-module references.
* The `Import` attribute is no longer supported for variables, because the AWS Parameter Store is no longer used for cross-module references.

Also, variables are no longer always added to function environments. Instead, the scope of a variable is controlled by the `Scope` attribute (see below). The old behavior caused too many cases of circular dependencies, which are always tedious to diagnose.

### λ# Tool

Function folders no longer need to be prefixed with the module name. They will still be found for backwards compatibility, but the new recommended naming is to just use the function name as folder name.

With the introduction of `--tool-profile` there was the need to rename `--profile` to `--aws-profile` to avoid ambiguity.

### λ# Assemblies

CloudFormation resources are now always resolved to the ARN of the created resource. Previously, created resources were resolved using the `!Ref` operation, which varies by resource type. This change was necessary to homogenize CloudFormation resources with resource references found in input parameters and cross-module references. For convenience, the `AwsConverters` class contains methods for extracting resource names from S3/SQS/DynamoDB/etc. ARNs.

The `ALambdaFunction<TRequest>` base class was removed in favor of `ALambdaFunction<TRequest, TResponse>`.



VVVVV ***CONTINUE HERE*** VVVV


## New λ# Tool Features

* multi-stage deployment (build, publish, deploy)
* CloudWatch Logs
    * clean-up on function deletion
    * log retention limit
* dotnet global tool
* `lash setup`
* `lash config`: configure tool for deployments
* `lash deploy`: run create-stack command with required parameters
    * check if deployed module name matches
    * check if deployed module version is greater
    * module reference for deployment `ModuleName:*`
    * check that `ModuleName` matches the deployment
    * check that the deployed module is the same version or newer
    * use `--force-deploy` to deploy anyway
* `lash build`: compile all .Net projects and `Module.yml` file
* `lash publish`: unzip and copy assets from zip file to deployment S3 bucket
* `lash info`
    * show `dotnet` tool version
    * show `git` tool version
    * hide sensitive information (account id) unless `--show-sensitive` option is used
* `lash encrypt`
* input path can be a directory instead of the module file (will look for `Module.yml`)
* javascript functions

* parameter file
    * ability to add cloudformation template parameters when deploying a module
    * ability to use secrets as cloudformation parameters (`Secret:` attribute)
    * translate aliases to ARNs in parameters.yml file under the `ModuleSecrets`
    * ability to specify a list of input parameters for a key
        ```yaml
        MySecret: xyz
        ModuleSecrets:
            - arn:...
            - alias/...
        ```
* cli option `--tool-profile` or use environment variable `LAMBDASHARP_PROFILE`; if not provided, defaults to `Default`

* `ModuleSecrets` input
* `Module::Id`
* `Module::Name`
* `Module::Version`
* `Module::DeadLetterQueueArn`
* `Module::LoggingStreamArn`

## New λ# Module Features

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
* add `Module::Name` and `Module::Version` and `Module::Id`
* `Variables`
    * add `Scope:` attribute to variables (can either be `*` or a list of function names)

* allow `!Ref` for alexa skill
* substitute parameter values in `!Ref` and `!Sub` operations
    * find and replace `!Ref` parameter references in-place rather than to letting cloudformation replace them for us

## New λ# Environment Features
* module registration (similar to what we did with rollbar)
* configurable (LambdaSharp Module)
    * `LoggingStreamRetentionPeriod`
    * `DefaultSecretKeyRotationEnabled`

## New λ# Assembly Features
* `ALambdaFunction` now has `DecryptSecretAsync()` and `EncryptSecretAsync()` methods (uses DefaultSecretKey by default)

## New λ# Assemblies Features

* `string DecryptSecretAsync(string secret, Dictionary<string, string> encryptionContext = null)`
* `string EncryptSecretAsync(string text, string encryptionKeyId = null, Dictionary<string, string> encryptionContext = null)`
* `T T DeserializeJson<T>(Stream stream)`
* `T DeserializeJson<T>(string json)`
* `string SerializeJson(object value)`
## Fixes

## Internal Changes

* change `${Tier}-` to `${DeploymentPrefix}`
    * should `Tier` include the trailing `-`? it would make it more like a namespace prefix
* need to create module `manifest.json` file that contains the version, module name, and dependencies
* always add `ModuleVersion` to output values
* `cloudformation-v{version}-{md5}.json`
* there is now the notion of a module ID, which is the instance name of the module
