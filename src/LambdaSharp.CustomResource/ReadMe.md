# LambdaSharp.CustomResource

This package contains interfaces and classes used for building Lambda functions on AWS to manage custom resources in CloudFormation. This package extends the functionality provided the [LambdaSharp](https://www.nuget.org/packages/LambdaSharp/) package.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## ALambdaCustomResourceFunction<TProperties, TAttributes>

The `ALambdaCustomResourceFunction<TProperties, TAttributes>` base class implements the CloudFormation protocol for custom resources. The `TProperties` type parameter defines the properties for initializing the custom resource. The `TAttributes` type parameter defines the attributes of the created resource that can be accessed by CloudFormation.

```csharp
public sealed class Function : ALambdaCustomResourceFunction<ResourceProperties, ResourceAttributes> {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // TO-DO: add function initialization and reading configuration settings
    }

    public override async Task<Response<ResourceAttributes>> ProcessCreateResourceAsync(Request<ResourceProperties> request, CancellationToken cancellationToken) {

        // TO-DO: create custom resource using resource properties from request

        return new Response<ResourceAttributes> {

            // TO-DO: assign a physical resource ID for custom resource
            PhysicalResourceId = "MyResource:123",

            // TO-DO: set response attributes
            Attributes = new ResourceAttributes { }
        };
    }

    public override async Task<Response<ResourceAttributes>> ProcessDeleteResourceAsync(Request<ResourceProperties> request, CancellationToken cancellationToken) {

        // TO-DO: delete custom resource identified by PhysicalResourceId in request

        return new Response<ResourceAttributes>();
    }

    public override async Task<Response<ResourceAttributes>> ProcessUpdateResourceAsync(Request<ResourceProperties> request, CancellationToken cancellationToken) {

        // TO-DO: update custom resource using resource properties from request

        return new Response<ResourceAttributes> {

            // TO-DO: optionally assign a new physical resource ID to trigger deletion of the previous custom resource
            PhysicalResourceId = "MyResource:123",

            // TO-DO: set response attributes
            Attributes = new ResourceAttributes { }
        };
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
