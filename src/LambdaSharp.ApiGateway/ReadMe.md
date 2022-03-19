# LambdaSharp.ApiGateway

This package contains interfaces and classes used for building Lambda functions on AWS which integrate with [API Gateway and API Gateway V2](https://docs.aws.amazon.com/apigateway/latest/developerguide/welcome.html). This package extends the functionality provided the [LambdaSharp](https://www.nuget.org/packages/LambdaSharp/) package.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## ALambdaApiGatewayFunction

The `ALambdaApiGatewayFunction` base class provides capabilities for mapping REST API and WebSocket endpoints defined in the LambdaSharp module file directly to methods of the class.

```csharp
public sealed class Function : ALambdaApiGatewayFunction {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // TO-DO: add function initialization and reading configuration settings
    }

    public async Task<AddItemResponse> AddItem(AddItemRequest request) {

        // TO-DO: add business logic for API Gateway resource endpoint
        return new AddItemResponse { };
    }
}
```

In the corresponding `Module.yml` file might look something like this:
```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 30
  Sources:
    - Api: POST:/items
      Invoke: AddItem
```

## ALambdaApiGatewayFunction (Proxy)

The `ALambdaApiGatewayFunction` base class can also be used to process generic API Gateway invocations.

```csharp
public sealed class Function : ALambdaApiGatewayFunction {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // TO-DO: add function initialization and reading configuration settings
    }

    public override async Task<APIGatewayProxyResponse> ProcessProxyRequestAsync(APIGatewayProxyRequest request) {

        // TO-DO: add business logic for API Gateway proxy request handling

        return new APIGatewayProxyResponse {
            Body = "Ok",
            Headers = new Dictionary<string, string> {
                ["Content-Type"] = "text/plain"
            },
            StatusCode = 200
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
