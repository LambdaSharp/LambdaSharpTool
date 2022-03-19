# LambdaSharp.Logging

This package is used by [LambdaSharp](https://www.nuget.org/packages/LambdaSharp/) and [LambdaSharp.App](https://www.nuget.org/packages/LambdaSharp.App/) to provide logging functionality to AWS Lambda functions and Blazor WebAssembly applications, respectively

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.

## Logging

The `ILambdaSharpLogger` interface defines logging methods to make it easy to report information to CloudWatch Logs and optionally to an error aggregator, such as Rollbar.

* `LogDebug()`: Log a debugging message. This message will only appear in the log when debug logging is enabled and will not be forwarded to an error aggregator.
* `LogInfo()`: Log an informational message. This message will only appear in the log and not be forwarded to an error aggregator.
* `LogWarn()`: Log a warning message. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogError()`: Log an exception as an error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogErrorAsInfo()`: Log an exception as an information message. This message will only appear in the log and not be forwarded to an error aggregator.
* `LogErrorAsWarning()`: Log an exception as a warning. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogFatal()`: Log an exception with a custom message as a fatal error. This message will be reported if an error aggregator is configured for the <c>LambdaSharp.Core</c> module.
* `LogMetric()`: Log a CloudWatch metric. The metric is picked up by CloudWatch logs and automatically ingested as a CloudWatch metric.

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
