![λ#](Docs/LambdaSharp_v2_small.png)

# LambdaSharp Tool & Framework (v0.3)

**[Read what's new in the latest release.](Docs/ReleaseNotes-Cebes.md)**

The objective of λ# is to accelerate the innovation velocity of serverless solutions. Developers should be able to focus on solving business problems while deploying scalable, observable solutions that follow DevOps best practices.

λ# is a .NET Core 2.x framework and tooling for rapid application development, deployment, and management of [AWS Lambda](https://aws.amazon.com/lambda/) functions and serverless infrastructure. Resources are automatically converted into parameters for easy access by AWS Lambda C# functions. Furthermore, λ# modules are composable by exchanging resource references using the [AWS Systems Manager Parameter Store](https://aws.amazon.com/systems-manager/features/).

When creating a λ# module, you only need to worry about three files:
* The AWS Lambda C# code
* The .NET Core project file
* The λ# module file

## Getting Started

**[Before getting started, you must setup your λ# Environment.](Bootstrap/)**

Once setup, creating modules with Lambda functions and deploying them only requires a few steps.

```bash
# Create a new λ# module
lash new module MySampleModule

# Add a function to the λ# module
lash new function MyFunction

# Deploy the λ# module
lash deploy
```

The λ# tool uses a YAML file to compile the C# projects, upload assets, and deploy the CloudFormation stack in one step. The YAML file describes the entire module including the parameters, resources, and functions.

```yaml
Name: MySampleModule

Version: 1.0

Functions:

 - Name: MyFunction
   Memory: 128
   Timeout: 30
```

The C# project contains the Lambda handler.

```csharp
namespace MySampleModule.MyFunction {

    public class FunctionRequest {

        // TODO: add request fields
    }

    public class FunctionResponse {

        // TODO: add response fields
    }

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request, ILambdaContext context) {

            // TODO: add business logic

            return new FunctionResponse();
        }
    }
}
```

## Learn More

1. [Setup λ# Environment **(required)**](Bootstrap/)
1. [λ# Samples](Samples/)
1. [Module File Reference](Docs/ModuleFile.md)
1. [Folder Structure Reference](Docs/FolderStructure.md)
1. [λ# Tool Reference](src/MindTouch.LambdaSharp.Tool/)
1. [Release Notes](Docs/ReadMe.md)

## License

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
