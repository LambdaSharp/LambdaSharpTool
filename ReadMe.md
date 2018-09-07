![λ#](Docs/LambdaSharp_v2_small.png)

# LambdaSharp Tool & Framework (Beta)

The objective of λ# is to accelerate the innovation velocity of serverless solutions. Developers should be able to focus on solving business problems while deploying scalable, observable solutions that follow DevOps best practices.

λ# is a .NET Core 2.x framework and tooling for rapid application development, deployment, and management of [AWS Lambda](https://aws.amazon.com/lambda/) functions and serverless infrastructure. Resources are automatically converted into parameters for easy access by AWS Lambda C# functions. Furthermore, λ# modules are composable by exchanging resource references using the [AWS Systems Manager Parameter Store](https://aws.amazon.com/systems-manager/features/).

When creating a λ# module, you only need to worry about three files:
* The AWS Lambda C# code
* The .NET Core project file
* The λ# module file

__Example__

The following AWS Lambda function listens to a [Slack](https://slack.com) command request and responds with a simple message. `ASlackCommandFunction` is one of several base classes that can be used to create lambda functions easily and quickly.

```csharp
namespace GettingStarted.SlackCommand {

    public class Function : ALambdaSlackCommandFunction {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        protected async override Task HandleSlackRequestAsync(SlackRequest request)
            => Console.WriteLine("Hello world!");
    }
}
```

The λ# deployment tool uses a YAML file to compile the C# projects, upload assets, and deploy the CloudFormation stack in one step. The YAML file describes the entire module including the parameters, resources, and functions.

```yaml
Name: GettingStarted

Description: Sample module that shows a Slack integration

Functions:
  - Name: SlackCommand
    Description: Respond to slack commands
    Memory: 128
    Timeout: 30
    Sources:
      - SlackCommand: /slack
```

# Learn More

1. [Setup λ# Environment **(required)**](Bootstrap/)
1. [λ# Samples](Samples/)
1. [Module File Reference](Docs/ModuleFile.md)
1. [Folder Structure Reference](Docs/FolderStructure.md)
1. [λ# Tool Reference](src/MindTouch.LambdaSharp.Tool/)

# Releases

Releases are named after Greek philosophers.

## Cebes (v0.3-RC1) - 2018-09-06

* **Breaking Change:** Default module file name was changed to `Module.yml` from `Deploy.yml`
* **Breaking Change:** S3 event subscriptions are now handled by a custom resource handler, which allows S3 event subscriptions to work with new and existing S3 buckets. However, this change is not backwards compatible with existing S3 event subscriptions.
* Updated [Bootstrap](Bootstrap/) procedure.
* Added support for short form CloudFormation intrinsic functions, such as <code>!Ref</code>, <code>!Join</code>, <code>!Sub</code>, etc.
* Added support parameter collections. All parameter types can have nested parameters.
* Added support for CloudFormation Macro sources. See [CloudFormation Macro](Samples/MacroSample/) sample.
* Added support for Kinesis Stream sources. See [Kinesis Stream](Samples/KinesisSample/) sample.
* Added support for DynamoDB Stream sources. See [DynamoDB Stream](Samples/DynamoDBSample/) sample.
* Improved how Lambda function parameters are passed in. Instead of relying on an embedded `parameters.json` file, parameters are now passed in via environment variables. This means that Lambda function packages no longer need to be re-uploaded because of parameter changes.
* Included `Sid` attribute for all built-in, automatic permissions being added to provide more context.
* Switched to native AWS Lambda `JsonSerializer` class for serializing/deserializing data.
* Fixed an issue where some CloudFormation properties needed to suffixed with `_` to work with [Humidifier](https://github.com/jakejscott/Humidifier) library for generating CloudFormation templates correctly.
* Upgraded [YamlDotNet](https://github.com/aaubry/YamlDotNet) library to 5.0.1

## Brontinus (v0.2) - 2018-08-13

* Revised λ# nomenclature, which introduced breaking changes for the module files.
* Added support for Alexa Skill invocation sources.
* Added custom resource handler for deploying file packages to S3 buckets.
* Added command for listing deployed λ# modules.
* Added default warning/error logging SNS topic for all Lambda functions.
* Streamlined the λ# Environment setup procedure.

## Acrion (v0.1) - 2018-07-17

Initial release of λ#.

# License

> Copyright (c) 2018 MindTouch
>
> Licensed under the Apache License, Version 2.0 (the "License");
> you may not use this file except in compliance with the License.
> You may obtain a copy of the License at
>
> http://www.apache.org/licenses/LICENSE-2.0
>
> Unless required by applicable law or agreed to in writing, software
> distributed under the License is distributed on an "AS IS" BASIS,
> WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
> See the License for the specific language governing permissions and
> limitations under the License.
