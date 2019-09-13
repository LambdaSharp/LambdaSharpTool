---
title: LambdaSharp "Eurytus" Release (v0.5)
description: Release notes for LambdaSharp "Eurytus" (v0.5)
keywords: release, notes, eurytus
---
# LambdaSharp "Eurytus" Release (v0.5.0.3) - 2019-03-08

> Eurytus was an eminent Pythagorean philosopher. He was a disciple of Philolaus, and Diogenes Laërtius mentions him among the teachers of Plato, though this statement is very doubtful. [(Wikipedia)](https://en.wikipedia.org/wiki/Eurytus_(Pythagorean))

## What's New

The objective of the LambdaSharp 0.5 _Eurytus_ release has been to streamline the iterative development process with the LambdaSharp CLI. The biggest time sink for deploying CloudFormation templates is that errors are only detected during the _deploy_ phase. Consequently, the biggest time optimization is to detect errors as early as possible. This pushes the LambdaSharp CLI into the validating compiler territory, which has always been the objective. With this release, the LambdaSharp CLI validates all properties and attributes for all AWS types. That means, if you forget to set a required property on a resource or you have a typo, the LambdaSharp CLI will detect it during the _build_ phase of the module.

Streamlining error detection would be incomplete if it did not apply to published modules as well. During the _build_ phase, the LambdaSharp CLI downloads the manifests of referenced modules and uses them to validate properties and attributes of custom resource types. Then, during the _deploy_ phase, the CLI checks for the presence of the referenced modules and automatically installs them if needed. This release includes three utility modules: `LambdaSharp.S3.IO` provides functionality for writing or deleting files from S3 buckets, `LambdaSharp.S3.Subscriber` enables Lambda functions to receive notifications from S3 buckets, and `LambdaSharp.Twitter.Query` enables Lambda functions to received notifications from Twitter queries.

In addition to the CLI improvements, LambdaSharp modules have also received a few new features. Amongst them is support for `Mapping` and `Condition` definitions, which fill the void to provide complete coverage of all CloudFormation constructs. More exciting is built-in support for decrypting parameters and variables directly in CloudFormation. Also, modules now have a constructor/destructor called a `Finalizer` allowing for dynamic initialization and clean-up operations.

Finally, as a bonus feature, the LambdaSharp CLI now has the utility command `util delete-orphan-lambda-logs` to delete legacy Lambda CloudWatch Logs. This feature is particularly useful for those who have been experimenting with Lambda in the past. By default, CloudWatch Logs are never deleted and keep accumulating over time, only increasing your monthly AWS bill. LambdaSharp modules are quite different. They have default retention policy of 30 days for logs and automatically delete them on tear down.


## BREAKING CHANGES

The following change may impact modules created with previous releases.


### Module Definition

* The top-level `Module` attribute must now contain at least one period (`.`) to denote the _module namespace_ and _module name_.
* The `Inputs`, `Outputs`, `Variables`, and `Functions` sections of LambdaSharp module have been combined into a single `Items` section to give more organizational freedom.
* The `Export` definitions have been removed in favor of a special class for `Scope` on items that can be exported.
* Cross-module references are now grouped into `Using` definitions by module they import from.
* `Package` definitions now compress local files into a deployed zip package only. The zip package can then be used to deploy files to S3--using the `LambdaSharp::S3::Unzip` resource--, create [Lambda Layers](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/LambdaLayerSample), Alexa skills, or anything else that requires a compressed zip package as its input.
* The `Var` definition has been split into two distinct definitions: `Variable` for fixed values and expressions, and `Resource` for existing or new resources.
* The `CustomResource` definition has been renamed to `ResourceType` and enhanced with a specification about its properties and attributes to enable validation at compilation time.
* `Function` definitions lost their `VPC` section and `ReservedConcurrency` attribute. Instead, like `Resource` definitions, they have a `Properties` section that can be used to set these, and other settings, directly. This approach is much more future proof.
* Nesting items has been limited to `Using` and `Group` definitions.
* The default value for the `Version` attribute is now `1.0-DEV`.


### LambdaSharp Runtime

* The _LambdaSharp Runtime_ has been renamed to _LambdaSharp Core_. The previous terminology caused too much confusion with the AWS Lambda runtime.
* The LambdaSharp Core is deployed as a single module without using nested modules. This decreases the deployment time.


### LambdaSharp CLI

The big change is to run `lash` now instead of `dotnet lash`. It's shorter and works just as well!

* Build process
    * Renamed `--skip-assembly-validation` option to `--no-assembly-validation`
    * Renamed `--cf-output` option to `--cfn-output`
* Publish process
    * Modules are now published to a new location in the S3 deployment bucket: `${Module::Namespace}/Modules/${Module::Name}/Versions/${Module::Version}`
    * The LambdaSharp manifest is now embedded in the CloudFormation template inside the `Metadata` section under `LambdaSharp::Manifest`.
* Deploy Process
    * Renamed `--inputs` option to `--parameters` for consistency reasons.
    * Removed `--input` option.


### LambdaSharp Assemblies

* Assemblies
    * Merged `MindTouch.LambdaSharp.CustomResource` assembly into the `MindTouch.LambdaSharp` assembly.
    * Renamed `MindTouch.LambdaSharp` assembly to `LambdaSharp`.
    * Renamed `MindTouch.LambdaSharp.Slack` assembly to `LambdaSharp.Slack`.
    * Renamed `MindTouch.LambdaSharp.Tool` assembly to `LambdaSharp.Tool`.
* Namespaces and Classes
    * Renamed the `MindTouch.LambdaSharp` namespace to `LambdaSharp`.
    * Renamed `ALambdaEventFunction` to `ALambdaTopicFunction`.


## New LambdaSharp Module Features

### Module Namespace

The `Module` attribute now specifies both the module namespace and the module name. The module namespace could be the name of an organization, an individual, or a project the module is part of. The module namespace is used to group related modules in the deployment S3 bucket. Everything after the first period is considered to be part of the module name, including additional periods.

In the following example, `Acme` is the module namespace and `Accounting.Reports` is the module name:
```yaml
Module: Acme.Accounting.Reports
```

### Module Dependencies

LambdaSharp modules now support the concept of dependencies. Used modules are listed in the `Using` section of the module definition. Listing a module impacts both the build and deploy phases. During the build phase, the module manifests are imported to help with validation. During the deploy phase, used dependencies are checked for and deployed when missing.

In the following example, the module definition indicates that it has a dependency on `LambdaSharp.S3.IO`.
```yaml
Module: Acme.MyModule
Using:

  - Module: LambdaSharp.S3.IO
```

### Module Items

All definitions in a LambdaSharp module are now unified in the `Items` section. This change allows definitions to be more freely organized. For example, it is now possible to group different definition types (parameters, variables, etc) by their purpose, rather than by their type. Definitions can further be grouped into namespaces to better reflect the organization of the module.

The following example shows how definitions can be placed close to where they are needed or close to other definitions they are related to. This design gives greater flexibility to developers to organize their thoughts, not unlike what is found in most programming languages.
```yaml
Module: Acme.MyModule
Items:

  - Parameter: PersonName
    Type: String

  - Variable: GreetPerson
    Type: String
    Scope: Reporting::Calculator
    Value: !Sub "Hi ${PersonName}"

  - Group: Reporting
    Items:

      - Resource: Bucket
        Scope: Reporting::Calculator
        Type: AWS::S3::Bucket

      - Function: Calculator
        Memory: 128
        Timeout: 30

  - Variable: AccountingBucket
    Scope: public
    Value: !GetAtt Reporting::Bucket.Arn
```

### `Secret` Type

`Parameter` and `Variable` definitions with encrypted values (type `Secret`) can now be decrypted by accessing a dynamically created nested item called `Plaintext`.

The following example defines an encrypted parameter called `MySecretParameter`. For illustrative purposes, the variable `MyDecryptedValue` shows how easy it is to access the plaintext value. The `!Ref` expression could be used in any context where an expression can be used. Note that the module must have been granted access to the encryption key used by the encrypted value.

```yaml
- Parameter: MySecretParameter
  Type: Secret

- Variable:
  Value: !Ref MySecretParameter::Plaintext
```

### Module Finalizer

LambdaSharp modules can now have a _finalizer_ function that is automatically run after all resources have been created or before any resources are torn down. There are many use-cases for a module finalizer, including:
* Initialize a DynamoDB table with seed data on initial module deployment.
* Send an module availability notification to other services.
* Migrate existing data after a module has been updated to a newer version.
* Delete dynamically created resources when the module is torn down.
* Delete objects from an S3 bucket so it can be deleted when the module is torn down.

A module finalizer function must be called `Finalizer` and must appear in the top-level items of the module. The module finalizer timeout is always set to the maximum duration of 15 minutes. On invocation, the module finalizer receives the module version to allow it to track upgrades or downgrade scenarios. It also receives the module checksum so it track state for each CloudFormation update.

The following example shows how easy it is to define a module finalizer:
```yaml
- Function: Finalizer
  Memory: 128
```

### Function `Properties` Section

The `Function` definition now exposes its `Properties` section to access advanced functionality of Lambda functions, such as VPC configuration and Lambda layers. Similar to `Resource` definitions, the function `Properties` are validated during compilation.

The following example shows how to configure a Lambda function for VPC:
```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 30
  Properties:
    VpcConfig:
      SecurityGroupIds: !Ref SecurityGroupIds
      SubnetIds: !Ref SubnetIds
```

The next example shows how to set Lambda layers for a function:
```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 30
  Properties:
    Layers:
      - !Ref MyLambdaLayer
```

### Nested Modules

Nested modules are similar to nested CloudFormation stacks. The module reference is resolved at compile time to a CloudFormation template location. Furthermore, the LambdaSharp CLI seamlessly injects the deployment tier parameters required for deploying modules. [See `Nested` documentation](~/syntax/Module-Nested.md).

During the _build_ phase, the LambdaSharp CLI validates that all required parameters are supplied and that the supplied parameters exist.

The following example shows how to create a nested module definition and access its output values:
```yaml
- Nested: MyNestedModule
  Module: Acme.MyOtherModule:1.0
  Parameters:
    Message: !Sub "Hi from ${Module::Name}"

- Variable: NestedOutput
  Value: !GetAtt MyNestedModule.Outputs.OutputName
```

### `Condition` Item

LambdaSharp modules now support `Condition` items and the corresponding `!Condition` function. The `If` attribute is used on `Resource` and `Function` items to indicate that they are conditional.

The following example shows a `Condition` item keying off a `Parameter` and thus controlling the creation of two conditional resources.
```yaml
- Parameter: EnvType
  Description: Environment type.
  Default: test
  Type: String
  AllowedValues:
    - prod
    - test
  ConstraintDescription: must specify prod or test.

- Resource: EC2Instance
  Type: "AWS::EC2::Instance"
  Properties:
    ImageId: ami-0ff8a91507f77f867

- Group: ProductionResources
  Items:

    - Condition: Create
      Value: !Equals [ !Ref EnvType, prod ]

    - Resource: MountPoint
      Type: AWS::EC2::VolumeAttachment
      If: ProductionResources::Create
      Properties:
        InstanceId: !Ref EC2Instance
        VolumeId: !Ref ProductionResources::NewVolume
        Device: /dev/sdh

    - Resource: NewVolume
      Type: AWS::EC2::Volume
      If: ProductionResources::Create
      Properties:
        Size: 100
        AvailabilityZone: !GetAtt EC2Instance.AvailabilityZone
```

The `If` attribute can either have the name of a `Condition` item or contain the conditional expression directly. The following two examples are identical:
```yaml
- Condition: Create
  Value: !Equals [ !Ref CreateTopic, yes ]

- Resource: ConditionalTopic
  Type: AWS::SNS::Topic
  If: ProductionResources::Create
```
-VS-
```yaml
- Resource: ConditionalTopic
  Type: AWS::SNS::Topic
  If: !Equals [ !Ref CreateTopic, yes ]
```

Conditional resources that also have a `Scope` attribute are conditionally injected into their specified Lambda functions. Care needs to be taken when reading them from the function configuration to allow for them to be missing.

### `Mapping` Item

LambdaSharp modules now also support `Mapping` items and the corresponding `!FindInMap` function.

The next example shows a definition of a `Mapping` item and its use:
```yaml
- Mapping: Greetings
  Description: Time of day greeting
  Value:
    Morning:
      Text: Good morning
    Day:
      Text: Good day
    Evening:
      Text: Good evening
    Night:
      Text: Good night

- Parameter: SelectedTime
  Description: Parameter for selecting the time of day
  AllowedValues:
    - Morning
    - Day
    - Evening
    - Night

- Variable: SelectedGreeting
  Description: Selected greeting
  Value: !FindInMap [ Greetings, !Ref SelectedTime, Text ]
```

### Function Sources
* `DynamoDB` can now be a `!Ref` expression.
* `Kinesis` can now be a `!Ref` expression.
* `Queue` can now be a `!Ref` expression.
* `S3` can now be a `!Ref` expression.
* `Schedule` can now be a `!Ref` expression.
* `Topic` can now be a `!Ref` expression.
* `Topic` source now supports `Filters` to filter on SNS notifications.


## New LambdaSharp CLI Features

### Build Command

The LambdaSharp CLI now validates the properties of created resources and access to their attributes. This avoids common errors like missing a required property or having a simple typo in the definition. Similarly, when accessing an attribute with `!GetAtt`, the LambdaSharp CLI checks that the attribute exists on the resource. The validation for a resource can be suppressed with the `no-type-validation` [resource pragma](~/syntax/Module-Pragmas.md), which is useful when new resource types, properties, or attributes become available that are not yet supported by the LambdaSharp CLI.

The manifests for modules referenced as dependencies or nested modules are now downloaded to validate their usage. Manifests contain information about module parameters, resource type definitions, and output values. This enables the LambdaSharp CLI to validate properties and attributes on custom resource types just like with built-in AWS types. This feature can be disabled with the `--no-dependency-validation` [CLI option](~/cli/Tool-Build.md). The list of S3 buckets that are scanned for modules is controlled by a parameter in the AWS Systems Manager parameter store. By default, it is the deployment bucket and the lambdasharp bucket.

The LambdaSharp CLI now also analyzes the usage of all parameters, resources, and variables. If a declared parameter is not used anywhere, the LambdaSharp CLI will issue a warning to draw attention to it. This situation commonly occurs because of a missing `Scope` attribute or when a parameter is no longer needed, but its definition lingers. Internally, the analysis is also used to _garbage collect_ optional resource definitions. For example, every module has an embedded IAM Role (i.e. `Module::Role`) and API Gateway (i.e. `Module::RestApi`). However, unless these are used, the LambdaSharp CLI will automatically remove them to optimize the produced CloudFormation template.

Another benefit of analyzing expressions during the _build_ phase is the ability to inline variables into `!Sub` and `!Join` expressions. For example, the generated value for `Expression` is `"My list: First,Second,Third"`:
```yaml
- Variable: AConstant
  Value: First

- Variable: AListOfValues
  Value:
    - !Ref AConstant
    - Second
    - Third

- Variable: Expression
  Value: !Sub "My list: ${AListOfValues}"
```

The the introduction of `Condition` definitions also requires more careful validation of `!Ref` and `!GetAtt` expressions since these could refer to resources that may not exist during the _deploy_ phase. The LambdaSharp CLI now provides very basic support for validating conditional references. However, because the support is basic, it can lead to false negatives. Consequently, detected violations are shown as warnings instead of errors.

Finally, the entry point for .NET Core Lambda functions is now validated after compilation of the assembly. This avoids deploying a Lambda function that immediately fails, because the Lambda runtime was unable to located the entry point. This error usually occurs after refactoring code and forgetting to update the project file with the new namespace information. Although not a common error, it is extremely time consuming, because it occurs so late in the development process. Entry point validation can be skipped by using the `--no-assembly-validation` [CLI option](~/cli/Tool-Build.md) or the `no-assembly-validation` [function pragma](~/syntax/Module-Pragmas.md).

### Publish Command

The LambdaSharp CLI now prevents a module from being published again with the same version number. However, this constraint is lifted for pre-release versions. A pre-release version has a suffix string, such as `1.0-DEV`. If required, the LambdaSharp CLI can be forced to overwrite an existing published module by using the  `--force-publish` option. This behavior is the building block for caching of module manifests in the future. Republishing a module with breaking changes will cause issues. However, modules with pre-release versions are never cached and are therefore safe to be republished.

### Deploy Command

The LambdaSharp CLI now uses [CloudFormation Change Sets](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-updating-stacks-changesets.html) to deploy modules. CloudFormation change sets are the preferred way to update infrastructure as they provide an additional review mechanism for detecting unwanted changes.

With [AWS X-Ray going GA just in time](https://aws.amazon.com/blogs/developer/aws-x-ray-support-for-net-core-is-ga/), the LambdaSharp CLI now has a new `--xray` option to enable service-call tracing with AWS X-Ray for all functions in the module.

In addition, the LambdaSharp CLI now prompts for missing module parameters before creating or updating the CloudFormation stack. This includes parameters required by modules referenced in the `Using` section. If the module was deployed previously, the LambdaSharp CLI will only prompt for parameters that were not set previously. Furthermore, the `--parameters` option can be used to supply parameter values for the module being deployed. Parameters with default values are not prompted unless the `--prompt-all` option is used.

Having an interactive prompt in a CI/CD setup is about the worst thing that can happen. To avoid this scenario, use the `--prompts-as-errors` option to convert prompts into reported errors instead. This is another way to catch early that something is amiss before anything happens.

The following example shows the deployment of the `Demos/TwitterNotifier` module:
```
LambdaSharp CLI (v0.5) - Deploy LambdaSharp module
Readying module for deployment tier 'Sandbox'

...

Configuration for LambdaSharp.Demo.TwitterNotifier (v1.0-DEV)
*** TWITTER SETTINGS ***
|=> TwitterApiKey [Secret]: API Key: AQICAHiCs...wn7WieBEC
|=> TwitterApiSecretKey [Secret]: API Secret Key: AQICAHiCs...BNOJIrKzo
|=> TwitterQuery [String]: Search query: LambdaSharp

Deploying stack: Sandbox-LambdaSharp-Demo-TwitterNotifier [LambdaSharp.Demo.TwitterNotifier:1.0-DEV]
=> Stack create initiated for Sandbox-LambdaSharp-Demo-TwitterNotifier [CAPABILITY_NAMED_IAM, CAPABILITY_AUTO_EXPAND]
REVIEW_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo-TwitterNotifier (User Initiated)
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo-TwitterNotifier (User Initiated)
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              TwitterNotify
CREATE_IN_PROGRESS                  LambdaSharp::Registration::Module                       Module::Registration
...
CREATE_IN_PROGRESS                  AWS::Logs::SubscriptionFilter                           Notify::LogGroupSubscription (Resource creation Initiated)
CREATE_COMPLETE                     AWS::Logs::SubscriptionFilter                           Notify::LogGroupSubscription
CREATE_COMPLETE                     AWS::Lambda::Permission                                 Notify::Source1Permission
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo-TwitterNotifier
=> Stack create finished
Stack output values:
=> Module: LambdaSharp.Demo.TwitterNotifier:1.0-DEV

Done (finished: 2/1/2019 2:19:28 PM; duration: 00:04:36.2478253)
```

A small, aesthetic win is that the LambdaSharp CLI uses the manifest to translate the resource names from their logical ID to their module definition name. Similarly, custom resource types are translated to their actual names. For example, update to resources of type `Custom::LambdaSharpRegisterFunction` are shown as `LambdaSharp::Register::Function` instead.

Last, but not least, all in resources in LambdaSharp module are not automatically tagged, when possible, with the following information:
|Tag Name|Description|Example|
|--------|-----------|-------|
`LambdaSharp:DeployedBy`|The IAM identity used to deploy the CloudFormation stack.|`user/bob`|
`LambdaSharp:Module`|The full module name without version or source bucket specification.|`My.Module`|
`LambdaSharp:RootStack`|The name of the root CloudFormation stack under which the resource are created. This is the same as `aws:cloudformation:stack-name` for non-nested modules. Otherwise, it is the stack name of the topmost, non-nested module.|`Sandbox-My-Module-MyResource-1TOI83RQZQZE3`|
`LambdaSharp:Tier`|The name of the deployment tier.|`Sandbox`|

### Config Command

The LambdaSharp CLI now allows to request a specific bucket name during the `config` command instead of defaulting to a name created by CloudFormation. The created bucket now also grants permission to the [AWS Serverless Repository](https://docs.aws.amazon.com/serverlessrepo/latest/devguide/what-is-serverlessrepo.html) to access its contents, in preparation to support publishing modules to it in the future.

Similar to the change the `deploy` command, the `config` command now prompts for new parameters when upgrading an existing configuration.

### New Command

The LambdaSharp CLI now allows to add a resource definition to a module, similar to the `new function` command. The new [`new resource` command](~/cli/Tool-NewResource.md) take a resource name and resource type. It then appends the a skeleont definition to the `Module.yml` file where the property values indicate the type of the property and if it is required.

```bash
lash new resource MyTopic AWS::SNS::Topic
```

Appends the following resource definition to the `Module.yml` file:
```yaml
- Resource: MyResource
  Description: TO-DO - update resource description
  # Scope: List of functions to be given the name of the resource
  Type: AWS::SNS::Topic
  # Allow: Shorthand or allowed actions
  Properties:
    DisplayName: String
    KmsMasterKeyId: String
    Subscription:
      - Endpoint: String # Required
        Protocol: String # Required
    TopicName: String
```

### Util Command

The LambdaSharp CLI now has a new `util` command menu with two sub-commands.

The first one is the `download-cloudformation-spec`, which is used by LambdaSharp contributors to update the built-in CloudFormation specification. It is not useful for non-contributors.

The second sub-command is `delete-orphan-lambda-logs`, which deletes orphaned Lambda CloudWatch log groups. Log groups created by LambdaSharp are automatically deleted when a module is torn down. However, by default, if you have experimented with Lambda functions before, the Lambda function CloudWatch log groups are never deleted. This sub-command scans all log groups and checks if the associated Lambda function still exists. If it does not, it deletes the CloudWatch log group.

Usage is straightforward. There is `--dryrun` option to have the sub-command show what it would do without affecting anything.
```bash
lash util delete-orphan-lambda-logs
```

The sub-command shows which CloudWatch log groups were deleted.
```
LambdaSharp CLI (v0.5) - Delete orphaned Lambda CloudWatch logs

* deleted '/aws/lambda/MyOldFunction'
* deleted '/aws/lambda/LifeBeforeLambdaSharpWasHardFunction'

Found 2 log groups. Deleted 2. Skipped 0.

Done (finished: 2/1/2019 2:57:08 PM; duration: 00:00:00.9142275)
```

### Misc

The LambdaSharp CLI now prints a date-timestamp after each command and how long it took to run. This makes it easy to see if it was run recently and thus avoiding re-running a command in the heat of a frantic development session.

The LambdaSharp CLI now captures information about the git branch during the _build_ phase in addition to the git SHA. If needed, the git branch information can also be supplied with the `--git-branch` option.

## New LambdaSharp Core Module Features

The LambdaSharp Core module provides the basic capabilities of a deployment tier and is required by every module. The LambdaSharp Core module no longer requires nested modules. Instead, additional functionality is automatically pulled in during the _deploy_ phase by the LambdaSharp CLI.

### Module LambdaSharp.S3.IO

This module defines three resource types for interacting with S3 buckets:
* `LambdaSharp::S3::EmptyBucket`: empty all contents of a bucket on module tear-down
* `LambdaSharp::S3::Unzip`: copy/update a package to an S3 bucket
* `LambdaSharp::S3::WriteJson`: write a JSON file to an S3 bucket

### Module LambdaSharp.S3.Subscriber

This module provides the `LambdaSharp::S3::Subscription` resource type required for receiving S3 bucket notifications. It is automatically referenced by the LambdaSharp CLI when a Lambda function uses an S3 bucket as its event source.

### Module LambdaSharp.Twitter.Query

This module runs a Twitter query on a regular interval and publishes newly found tweets on an SNS topic. The [`TwitterNotifier` demo module](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Demos/TwitterNotifier) shows how to subscribe to messages and reformat them for delivery.

## New LambdaSharp Assembly Features

### Class `ALambdaFinalizerFunction`

This new base class was introduced for creating a module finalizer. This base class defines the proper request and response data-structures. See the [Finalizer Example](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/FinalizerSample) for its use.

### Class `ALambdaCustomResourceFunction`

This base class was enhanced to support direct Lambda invocations and indirect SNS topic invocations. The detection of either protocol is done automatically and no additional steps are required.

## Internal Changes

There wre some additional internal changes listed here for sake of completeness:
* The default Lambda log group retention period was increased from 7 to 30 days.
* The `ModuleCloudWatchLogsRole` is now defined once in the LambdaSharp Core module and then re-used by all modules.
* The `ModuleName` and `ModuleVersion` module output values have been combined into a single `Module` output value.

## Fixes

### (v0.5.0.3) - 2019-03-08

* [Added support for `!GetConfig`, `!GetParam`, and `!GetEnv` in module parameters file](https://github.com/LambdaSharp/LambdaSharpTool/issues/96)
* [Fixed issue where the `DefaultSecretsKey` was not properly set for the functions in `LambdaSharp.Core`](https://github.com/LambdaSharp/LambdaSharpTool/issues/105)
* [Fixed error in `LambdaSharp.Core` log processor](https://github.com/LambdaSharp/LambdaSharpTool/issues/106)
* [Use ANSI colors for CloudFormation updates](https://github.com/LambdaSharp/LambdaSharpTool/issues/107)
* [Apply CFN-spec corrections from `aws-cloudformation/cfn-python-lint`](https://github.com/LambdaSharp/LambdaSharpTool/issues/108)
* [Added `LambdaSharp.Core` module documentation](https://github.com/LambdaSharp/LambdaSharpTool/issues/109)
* [Added `LambdaSharp.S3.Subscriber` module documentation](https://github.com/LambdaSharp/LambdaSharpTool/issues/110)

### (v0.5.0.2) - 2019-03-01

* [Allow `BatchSize` and `StartingPosition` attribute for event sources to be expressions](https://github.com/LambdaSharp/LambdaSharpTool/issues/98)
* [Make `publish` command idempotent](https://github.com/LambdaSharp/LambdaSharpTool/issues/99)
* [Fix `NullReferenceException` when compiling a module with an invalid CLI profile](https://github.com/LambdaSharp/LambdaSharpTool/issues/100)
* [Use global AWS Lambda Tools extension for dotnet CLI](https://github.com/LambdaSharp/LambdaSharpTool/issues/101)
* [Prevent CLI from exiting with an error due to a network glitch during a CloudFormation stack update](https://github.com/LambdaSharp/LambdaSharpTool/issues/102)

### (v0.5.0.1) - 2019-02-19

* [Add ability to create a package with a Linux executable on Windows](https://github.com/LambdaSharp/LambdaSharpTool/issues/92)
* [Fix `InvalidCastException` when setting `Secrets` in parameters file](https://github.com/LambdaSharp/LambdaSharpTool/issues/93)
* [Fix `S3Writer` function runs out of memory on large zip files](https://github.com/LambdaSharp/LambdaSharpTool/issues/94)
