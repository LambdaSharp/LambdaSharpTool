![λ#](Docs/LambdaSharp_v2_small.png)

# LambdaSharp (Beta)

λ# is a .NET Core 2.0 framework and tooling for streamlining the development, deployment, and management of [AWS Lambda](https://aws.amazon.com/lambda/) functions and serverless infrastructure.

When creating a λ# app, you only need to worry about three files:
* The AWS Lambda C# code
* The .NET Core project file
* The λ# deployment file

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

The λ# deployment tool uses a YAML file to compile, upload, and deploy the CloudFormation stack all in one step. The YAML describes the app, its parameters, resources, and functions.

```yaml
Version: "2018-07-04"

Name: GettingStarted

Description: Intro app that shows a Slack integration

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
1. [Deployment File Reference](Docs/DeploymentFile.md)
1. [Folder Structure Reference](Docs/FolderStructure.md)
1. [λ# Tool Reference](src/MindTouch.LambdaSharp.Tool/)

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
