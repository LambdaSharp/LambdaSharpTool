﻿![λ#](Docs/images/LambdaSharpBadge.png)

# LambdaSharp - Serverless .NET on AWS

**[Read what's new in the v0.8.2 "Hicetas" release.](https://lambdasharp.net/articles/ReleaseNotes-Hicetas.html)**

LambdaSharp is a CLI and framework for serverless .NET Core application development on AWS. LambdaSharp uses a simple declarative syntax to generate sophisticated CloudFormation templates that provide simple, yet flexible, deployment options.

The objective of LambdaSharp is to accelerate the development pace of serverless solutions while helping developers adhere consistently to best practices to create scalable, observable, and modular systems.

![λ# CLI](Docs/images/LashAnsiColor-WIP.gif)

## Install LambdaSharp CLI

The LambdaSharp CLI is installed as a .NET Global Tool.

```bash
dotnet tool install -g LambdaSharp.Tool
```

Once installed, a deployment tier must be initialized.
```bash
lash init --quick-start
```

## Deploy a LambdaSharp Module

Creating modules with Lambda functions and deploying them only requires a few steps.

```bash
# Create a new LambdaSharp module
lash new module MySampleModule

# Add a function to the LambdaSharp module
lash new function MyFunction --type generic

# Deploy the LambdaSharp module
lash deploy
```

The LambdaSharp CLI uses a YAML file to compile the C# projects, upload artifacts, and deploy the CloudFormation stack in one step. The YAML file describes the entire module including the inputs, outputs, variables, resources, and functions.

```yaml
Module: MySampleModule
Items:

 - Function: MyFunction
   Memory: 128
   Timeout: 30
```

The C# project contains the Lambda handler.

```csharp
namespace MySampleModule.MyFunction {

    public class FunctionRequest {

        // add request fields
    }

    public class FunctionResponse {

        // add response fields
    }

    public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

            // add business logic

            return new FunctionResponse();
        }
    }
}
```

## Learn More

### Getting Started

1. [Setup LambdaSharp](https://lambdasharp.net/articles/Setup.html)

### Articles

1. [Why LambdaSharp?](https://lambdasharp.net/articles/WhyLambdaSharp.html)
1. [Constructors/Destructors for CloudFormation](https://lambdasharp.net/articles/Finalizer.html)

### Samples

1. [Create a Serverless Chat with Cognito, WebSocket, DynamoDB, and Lambda](https://github.com/LambdaSharp/Chat)
1. [Create a Static Website with CloudFormation](https://github.com/LambdaSharp/StaticWebsite-Sample)
1. [Create Animated GIFs from Videos with AWS Lambda](https://github.com/LambdaSharp/GifMaker-Sample)
1. [Misc. LambdaSharp Samples](Samples/)

### References

1. [LambdaSharp CLI Reference](https://lambdasharp.net/cli/Index.html)
1. [LambdaSharp .NET SDK Reference](https://lambdasharp.net/sdk/Index.html)
1. [LambdaSharp Syntax Reference](https://lambdasharp.net/syntax/Index.html)
1. [LambdaSharp Module IAM Short-hands](src/LambdaSharp.Tool/Resources/IAM-Mappings.yml)
1. Modules
    1. [LambdaSharp.Core](https://lambdasharp.net/modules/LambdaSharp-Core.html)
    1. [LambdaSharp.S3.IO](https://lambdasharp.net/modules/LambdaSharp-S3-IO.html)
    1. [LambdaSharp.S3.Subscriber](https://lambdasharp.net/modules/LambdaSharp-S3-Subscriber.html)
    1. [LambdaSharp.Twitter.Query](https://lambdasharp.net/modules/LambdaSharp-Twitter-Query.html)
1. Resource Types
    1. [LambdaSharp::S3::EmptyBucket](https://lambdasharp.net/modules/LambdaSharp-S3-EmptyBucket.html)
    1. [LambdaSharp::S3::Subscription](https://lambdasharp.net/modules/LambdaSharp-S3-Subscription.html)
    1. [LambdaSharp::S3::Unzip](https://lambdasharp.net/modules/LambdaSharp-S3-Unzip.html)
    1. [LambdaSharp::S3::WriteJson](https://lambdasharp.net/modules/LambdaSharp-S3-WriteJson.html)

## License

> Copyright (c) 2018-2021 LambdaSharp (λ#)
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
