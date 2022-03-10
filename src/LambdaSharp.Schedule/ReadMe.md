# LambdaSharp.Schedule

This package contains interfaces and classes used for building Lambda functions on AWS which integrate with [scheduled CloudWatch Events](https://docs.aws.amazon.com/AmazonCloudWatch/latest/events/ScheduledEvents.html).

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## ALambdaScheduleFunction

The `ALambdaScheduleFunction` base class handles the deserialization of the scheduled event.

```csharp
public sealed class Function : ALambdaScheduleFunction {

    //--- Methods ---
    public override async Task InitializeAsync(LambdaConfig config) {

        // TO-DO: add function initialization and reading configuration settings
    }

    public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {

        // TO-DO: add business logic
    }
}
```

In the corresponding `Module.yml` file might look something like this:
```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 30
  Sources:
    - Schedule: rate(1 minute)
      Name: MyEvent
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
