# LambdaSharp.SimpleNotificationService

This package contains interfaces and classes used for building Lambda functions on AWS which integrate with [Amazon Simple Notification Service (SNS)](https://docs.aws.amazon.com/sns/latest/dg/welcome.html). This package extends the functionality provided the [LambdaSharp](https://www.nuget.org/packages/LambdaSharp/) package.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## ALambdaTopicFunction<TMessage>

The `ALambdaTopicFunction<TMessage>` base class deserializes the body of the SNS notification record into the specified type parameter. The notification record can be accessed using the `CurrentRecord` property.

```csharp
public sealed class Function : ALambdaTopicFunction<Message> {

    //--- Constructors ---
    public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // TO-DO: add function initialization and reading configuration settings
    }

    public override async Task ProcessMessageAsync(Message message) {

        // TO-DO: add business logic
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
