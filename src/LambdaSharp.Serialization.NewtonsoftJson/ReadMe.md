# LambdaSharp.Serialization.NewtonsoftJson

This package implements a Lambda JSON serializer using the popular [JSON.NET](https://www.newtonsoft.com/json) package. It is only recommended for legacy projects that require compatibility with JSON.NET annotations. New projects should use `LambdaSystemTextJsonSerializer` provided by the [LambdaSharp](https://www.nuget.org/packages/LambdaSharp/) package.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET applications and services on AWS.

## Serialization

The construtor takes an optional callback to customize the JSON.NET serialization settings.

```csharp
new LambdaSharp.Serialization.NewtonsoftJson(settings => {
    settings.NullValueHandling = NullValueHandling.Include;
})
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
