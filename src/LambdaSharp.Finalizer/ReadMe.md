# LambdaSharp.Finalizer

This package contains interfaces and classes used for building Lambda functions on AWS. The finalizer is a special kind of Lambda function that is invoked at the end of a CloudFormation stack creation or update operation, or at the beginning of a stack delete operation. This package extends the functionality provided the [LambdaSharp](https://www.nuget.org/packages/LambdaSharp/) package.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## ALambdaFinalizerFunction

The `ALambdaFinalizerFunction` base class implements the LambdaSharp protocol for CloudFormation stack create, update, and delete operations. This function is can be used to initialize data stores on creation or migrate data between versions.

```csharp
public sealed class Function : ALambdaFinalizerFunction {

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // TO-DO: add function initialization and reading configuration settings
    }

    public override async Task CreateDeploymentAsync(FinalizerProperties current, CancellationToken cancellationToken) {

        // TO-DO: add business logic when creating a CloudFormation stack
    }

    public override async Task UpdateDeploymentAsync(FinalizerProperties current, FinalizerProperties previous, CancellationToken cancellationToken) {

        // TO-DO: add business logic when updating a CloudFormation stack
    }

    public override async Task DeleteDeploymentAsync(FinalizerProperties current, CancellationToken cancellationToken) {

        // TO-DO: add business logic when deleting a CloudFormation stack
    }
}
```

## License

> Copyright (c) 2018-2022 LambdaSharp (λ#)
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
