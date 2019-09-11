# λ# - Damo (v0.4.0.4) - 2019-01-11

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
dotnet tool install -g LambdaSharp.Tool --version 0.4.*
```

As part of the λ# CLI setup procedure, the CLI must be configured for the AWS account. The configuration step creates a profile and resources required to deploy λ# modules. The profile information is stored in the AWS Parameter Store so that it can be shared with team members. Multiple CLI profiles can be configured when needed.
```bash
dotnet lash config
```

Initializing a deployment tier has been streamlined into a single command, which deploys the λ# runtime modules from a public bucket. Alternatively, for λ# contributors, the CLI can deploy a locally compiled version of the λ# runtime modules.
```bash
dotnet lash init --tier Sandbox
```

For complete instructions and options, check out the updated [setup documentation](~/articles/Setup.md).

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


### Other CLI Commands

#### Info Command

The `info` command was enhanced to show information about other installed tools that λ# CLI depends on, such as `dotnet` and `git`. In addition, sensitive information--like the AWS account ID--are hidden unless `--show-sensitive` option is used.

See the [updated documentation](~/cli/Tool-Info.md) for more details.

#### Encrypt Command

The `encrypt` command was added to make it easier to encrypt sensitive information. The command can either use a specific KMS key or use the default KMS key for the deployment tier.

See the [updated documentation](~/cli/Tool-Encrypt.md) for more details.

### New Function Command

The `new function` command now allows specifying the target language when adding a function.

See the [updated documentation](~/cli/Tool-NewFunction.md) for more details.


## New λ# Module Features

### Variables

The `Variables` section defines literal values and resources. Variables can either be used to build other variables or passed into Lambda functions using the `Scope` attribute. Variables can define plaintext values, secrets, packages, or resources. When defining resources, variables can grant the module IAM role the permissions to act upon the resources.

To scope a variable to all functions, use the wildcard value (`*`):
```yaml
- Var: My Variable
  Scope: all
  Value: Best variable ever
```

To scope a variable to specific functions, list them by name:
```yaml
- Var: MyVariable
  Scope:
    - MyFirstFunction
    - MyOtherFunction
  Value: Another best variable!
```

#### Reusable Parameters and Variables

λ# module parameters and variables can now be referenced by other variables and resource properties.

The following example shows how a variable can be used by another variable:
```yaml
- Var: Greeting
  Value: Hello

- Var: Message
  Value: !Sub "${Greeting} World!"
```

The same is also true for module parameters:
```yaml
- Parameter: Greeting
  Description: The greeting to use in the welcome message

# ...

- Var: Message
  Value: !Sub "${Greeting} World!"
```

Similarly, the `!Ref` expression can be used to reuse a variable or parameter.
```yaml
- Parameter: Topic

# ...

- Var: MyTopic
  Value: !Ref Topic
  Resource:
    Type: AWS::SNS::Topic
    Allow: Publish
```

**NOTE:** the previous example is very common and λ# provides a shorthand notation for it.
```yaml
- Parameter: Topic
  Resource:
    Type: AWS::SNS::Topic
    Allow: Publish
```

#### Nested Variables

λ# module variables can be organized into hierarchies. The value of a nested variable is accessed by creating a path to it using a double-colon as separator (`::`).
```yaml
- Var: Greetings
  Variables:

    - Var: Coming
      Value: Hello

    - Var: Leaving
      Value: Bye

- Var: Message
  Value: !Sub "${Greetings::Coming} World!"
```

#### Default Variables

Some module variables are always defined. These include:
* `Module::Id`: the name of the deployed CloudFormation stack
* `Module::Name`:  the name of the module
* `Module::Version`: the version of the module
* `Module::DeadLetterQueueArn`: the default dead-letter queue
* `Module::LoggingStreamArn`: the default function logging stream
* `Module::DefaultSecretKeyArn`: the default secret encryption key

The built-in variables can be accessed like other variables:
```yaml
- Var: UseBuiltInVariable
  Value: !Ref Module::Version

- Var: InlineBuiltInVariables
  Value: !Sub "${Module::Name} (v${Module::Version})"
```


### Module Inputs

λ# modules can now define parameters and imports (a.k.a. cross-module references) in the [`Inputs` section](~/syntax/Module-Parameter.md).

#### Module Parameters

λ# module parameters are modelled after [CloudFormation parameters](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/parameters-section-structure.html) and support all of their attributes. See the [parameters documentation](~/syntax/Module-Parameter.md) for more details.

In addition, a λ# module parameter can associate IAM permission with its parameter value, similar to variables.
```yaml
- Parameter: Topic
  Resource:
    Type: AWS::SNS::Topic
    Allow: Publish
```

This concept is taken one step further with conditional resources, which are only created when the module parameter is set to its default value. Otherwise the provided parameter value is used. In either case, the resource is associated with the requested IAM permissions.
```yaml
- Parameter: Topic
  Description: Provide the ARN for an existing topic or leave blank to create a new one.
  Default: ""
  Resource:
    Type: AWS::SNS::Topic
    Allow: Publish
    Properties:
      DisplayName: My Topic
```

**NOTE:** the `Properties` section is ignored if a non-default parameter value is used since no resource is being created.

#### Module Secret Parameters

In addition to the default CloudFormation parameter types, λ# modules can have parameters of type `Secret`. A secret parameter is passed in as base64-encoded string of the encrypted data (see [CLI `encrypt` command](~/cli/Tool-Encrypt.md)). To be able to decrypt the data, the KMS key must either be listed in the `Secrets` section or be passed in the `Secrets` parameter. Encrypted parameter values remain encrypted through the deployment process and are only decrypted in memory by the functions when accessed during initialization.

```yaml
- Parameter: MyApiKey
  Type: Secret
```

#### Module Imports (a.k.a. Cross-Module References)

λ# module imports enable a module to reference output values from another module. This mechanism is also known as _cross-module references_. Cross-module references are implemented using CloudFormation parameters, conditionals, exports, and the [`!ImportValue` function](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference-importvalue.html) to reference them. The complexity of cross-module references implementation is hidden behind a usage mechanism consistent with module parameters and variables.

The λ# implementation of cross-module references enables CloudFormation to resolve references at deployment time. However, they can also be redirected to a different module output value or be given an specific value instead. This capability makes it possible to have a default behavior that is mostly convenient, while enabling modules to be re-wired to import parameters from other modules, or to be given existing values for testing or legacy purposes.

```yaml
- Import: MyOtherModule::Topic
  Scope: all
  Description: Topic ARN for notifying users

# ...

- Var: MyTopic
  Value: !Ref MyOtherModule::Topic
```

The default value for the module import is `$MyOtherModule::Topic`, which instructs CloudFormation to translate the module import to `!ImportValue "MyOtherModule::Topic"` (**NOTE:** deployment prefix was omitted for brevity). Setting the import value to `$SomeModule::OtherTopic` instructs CloudFormation to translate the module import to `!ImportValue "SomeModule::OtherTopic"` instead. Alternatively, the import value could be set to an existing resource ARN, suh as `arn:aws:sns:use-east-1:123456789012:ExistingTopic`. In this case, CloudFormation will use the ARN directly without using `!ImportValue`.

Furthermore, module imports can associate IAM permissions to the import value.
```yaml
- Import: MyOtherModule::Topic
  Resource:
    Type: AWS::SNS::Topic
    Allow: Publish
```
See module imports documentation for more details.

#### CloudFormation Interface

Module parameters and imports can be modified when updating a CloudFormation stack. By default, the module parameters and imports are shown using labels derived from their names in a section labeled _Module Settings_. The automatically generated labels insert spaces between lowercase characters/digits and uppercase characters (e.g. `MyParameterValue` becomes `My Parameter Value`). However, parameters and imports can be organized into custom sections by using the `Section` attribute with a custom label using the `Label` attribute. Sections and labels are useful to make configuring modules post-deployment more user friendly.

```yaml
- Parameter: MyParameter
  Section: My Module Settings
  Label: My Favorite Parameter # (automatic label would have been "My Parameter")
```


### Module Outputs

λ# modules can have three kinds of outputs: exports, custom resources, and macros.

Module exports are converted into [CloudFormation export](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-stack-exports.html) values for top-level stacks. For nested stacks, the module exports are converted into CloudFormation stack outputs. This behavior prevents other modules from taking dependencies on nested stacks.
```yaml
- Var: MyQueue
  Resource:
    Type: AWS::SQS::Queue
    Allow: Send

# ...

- Export: QueueArn
  Description:
  Value: !GetAtt MyQueue.Arn
```
See module exports documentation for more details.

Custom resource definitions create new types of resources that can be used by other modules. Custom resources are a powerful way to expand the capabilities of modules beyond those provided by CloudFormation.
```yaml
- CustomResource: Accounting::Report
  Description: Custom resource for creating accounting reports
  Handler: AccountReportGenerator

# ...

- Function: AccountReportGenerator
  Memory: 128
  Timeout: 30
  Sources:
    - Topic: AccountReportGeneratorTopic
```
See custom resource documentation for more details.

A macro definition creates a CloudFormation macro for the deployment tier. The handler must be a Lambda function. Once deployed, the macro is available to all subsequent module deployments.
```yaml
- Macro: ToUpper
  Description: CloudFormation macro for converting a string to uppercase
  Handler: StringOpFunction

- Macro: ToLower
  Description: CloudFormation macro for converting a string to uppercase
  Handler: StringOpFunction

# ...

- Function: StringOpFunction
  Memory: 128
  Timeout: 15
```
See [macro documentation](~/syntax/Module-Macro.md) for more details.


### Misc

#### Alexa Source

The Alexa source definition now allows expressions when setting the Alexa Skill ID.
```yaml
- Parameter: AlexaSkillId
  Default: "*"

# ...

- Function: Function
  Memory: 128
  Timeout: 30
  Sources:
    - Alexa: !Ref AlexaSkillId
```

## New λ# Runtime Features

The λ# runtime is now a top-level CloudFormation stack with supporting modules deployed as nested stacks.

### λ# Registrar

The newest runtime module is the `Registrar`, which is responsible for registering modules and functions. Upon registration, function logs are centrally processed to detect warnings and errors.

The `Registrar` can optionally be configured to integrate with [Rollbar](https://rollbar.com) to create tracking projects on module deployment.

See the λ# CLI & Runtime documentation for more details.

## New λ# Assembly Features

### ALambdaFunction Class

The following methods were added to the `ALambdaFunction` abstract, base class:
* `string DecryptSecretAsync(string secret, Dictionary<string, string> encryptionContext = null)`<br/>
This method decrypts a base64-encoded string value.
* `string EncryptSecretAsync(string text, string encryptionKeyId = null, Dictionary<string, string> encryptionContext = null)`<br/>
This method encrypt a string into a base64-encoded string. By default, the method uses the `DefaultSecretKeyArn` KMS key to encrypt the string.
* `T T DeserializeJson<T>(Stream stream)`<br/>
This method deserializes a stream using the built-in AWS Lambda .Net SDK JSON deserializer.
* `T DeserializeJson<T>(string json)`<br/>
This method deserializes a string using the built-in AWS Lambda .Net SDK JSON deserializer.
* `string SerializeJson(object value)`<br/>
This method serializes an object into a JSON string using the built-in AWS Lambda .Net SDK JSON deserializer.


## Internal Changes

* The `${Tier}-` expression has been replaced with `${DeploymentPrefix}` in CloudFormation templates.
* Lambda CloudWatch Logs are now configured to self-delete log stream entries after seven (7) days. In addition, the log group is now deleted when the function is deleted during module tear-down.

## Fixes

### (v0.4.0.4) - 2019-01-11

* [Fixed LambdaSharp modules are always installed from public lambdasharp bucket](https://github.com/LambdaSharp/LambdaSharpTool/issues/82)
* [Fixed documentation error that showed wrong keyword for subscribing a function to a topic](https://github.com/LambdaSharp/LambdaSharpTool/issues/83)
* [Fixed cannot unzip package from deployment bucket when S3PackageLoader is installed from the global lambdasharp bucket](https://github.com/LambdaSharp/LambdaSharpTool/issues/84)
* [Fixed error when creating a resource with custom resource type](https://github.com/LambdaSharp/LambdaSharpTool/issues/85)

### (v0.4.0.3) - 2018-12-16

* [Fixed when Fn::Join always used "," to join items, no matter what separator was given](https://github.com/LambdaSharp/LambdaSharpTool/issues/77)
* [Fixed VersionInfo `operator !=` (not equal) returned `false` when the references were not equal](https://github.com/LambdaSharp/LambdaSharpTool/issues/78)
* [Fixed reference resolver sometimes failing to recognize legal expressions](https://github.com/LambdaSharp/LambdaSharpTool/issues/79)
* [Fixed invalid wildcard assembly reference in generated .csproj file](https://github.com/LambdaSharp/LambdaSharpTool/issues/80)

### (v0.4.0.2) - 2018-11-13
* [Fixed issue where λ# bucket discovery incorrectly defaulted back to the original bucket during deployment.](https://github.com/LambdaSharp/LambdaSharpTool/issues/60)
* [Fixed issue where AWS profile was only set via `AWS_PROFILE` environment variable. Now `AWS_DEFAULT_PROFILE` is also set.](https://github.com/LambdaSharp/LambdaSharpTool/issues/61)
* [Fixed issue where `config` did not default to `LAMBDASHARP_PROFILE` value when configuring a new CLI profile.](https://github.com/LambdaSharp/LambdaSharpTool/issues/62)
* [Fixed issue where λ# Runtime module used a multi-region domain name pattern for S3 buckets that was incompatible with the `us-east-1` region.](https://github.com/LambdaSharp/LambdaSharpTool/issues/63)

### (v0.4.0.1) - 2018-11-12
* [Fixed an issue where file packages did not get the correct name.](https://github.com/LambdaSharp/LambdaSharpTool/issues/57)
* [Fixed an issue where function could not be referenced by CloudFormation expressions (e.g. `!Ref`).](https://github.com/LambdaSharp/LambdaSharpTool/issues/58)
