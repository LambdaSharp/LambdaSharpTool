![λ#](~/images/LambdaSharpLogo.png)

# LambdaSharp (v[!include[LAMBDASHARP_VERSION](version.txt)]) - Serverless .NET on AWS

**[Read what's new in the 0.6 "Favorinus" release.](~/articles/ReleaseNotes-Favorinus.md)**

λ# is a command line tool and a framework for serverless application development. λ# uses a simple declarative syntax to generate sophisticated CloudFormation templates that provide simple, yet flexible, deployment options.

The objective of λ# is to accelerate the development pace of serverless solutions while helping developers adhere consistently to best practices to create scalable, observable, and modular systems.

## Give a Star! :star:

If you like or are using this project, please [give it a star on GitHub](https://github.com/LambdaSharp/LambdaSharpTool). Thanks!

![λ# CLI](images/LashAnsiColor-WIP.gif)

## Install λ# CLI

The λ# CLI is installed as a .NET Global Tool.

```bash
dotnet tool install -g LambdaSharp.Tool
```

Once installed, the λ# CLI needs to be configured.
```bash
lash config
```

Finally, a deployment tier must be initialized with the λ# Core.
```bash
lash init --tier Sandbox
```

## Deploy a λ# Module

Creating modules with Lambda functions and deploying them only requires a few steps.

```bash
# Create a new λ# module
lash new module MySampleModule

# Add a function to the λ# module
lash new function MyFunction

# Deploy the λ# module
lash deploy
```

The λ# CLI uses a YAML file to compile the C# projects, upload assets, and deploy the CloudFormation stack in one step. The YAML file describes the entire module including the inputs, outputs, variables, resources, and functions.

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

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

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

## License

```
Copyright (c) 2018-2019 LambdaSharp (λ#)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```