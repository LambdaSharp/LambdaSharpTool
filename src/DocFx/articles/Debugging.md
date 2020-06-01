---
title: Lambda Debug Logging - Lambda Functions - LambdaSharp
description: Description of how to enable debug logging for Lambda functions
keywords: debug, logs, cloudwatch
---

# Lambda Debugging

While it is not (yet) possible to attach a debugger to a running Lambda function, the [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) base class provides debug logging capabilities to enable better inspection capabilities of what's going in a deployed Lambda function.

## Debug Logging

Lambda functions deployed by LambdaSharp have a `DEBUG_LOGGING_ENABLED` environment variable to enable debug logging after deployment. By default, `DEBUG_LOGGING_ENABLED` is set to `false`.

At runtime, the value of the `DEBUG_LOGGING_ENABLED` environment variable can be checked through the [DebugLoggingEnabled](xref:LambdaSharp.ALambdaFunction.DebugLoggingEnabled) property.

Debug statements are logged by using [LogDebug(string format, params object[] arguments)](xref:LambdaSharp.ALambdaFunction.LogDebug(System.String,System.Object[])) method and appear in CloudWatch logs with a `*** DEBUG:` prefix:
```log
*** DEBUG: this entry is only emitted when debug logging is enabled
```

For complex debug statements, it is best practice to first check the [DebugLoggingEnabled](xref:xref:LambdaSharp.ALambdaFunction.DebugLoggingEnabled) property before invoking [LogDebug(...)](xref:LambdaSharp.ALambdaFunction.LogDebug(System.String,System.Object[])) . Checking if debug logging is enabled first avoids incurring wasteful overhead when the debug statement is discarded anyway.

```csharp
if(DebugLoggingEnabled) {
    LogDebug(CreateComplexDebugStatement());
}
```

## Enable Debug Logging

To enable debug logging, follow these steps:
1. Go to the AWS Console
1. Locate the Lambda function
1. Click _Edit_ next to the environment variables section
1. Change the value for `DEBUG_LOGGING_ENABLED` to `true`
1. Click _Save_.

This operation will restart the Lambda function with debug logging enabled.

## Request/Response Logging

The most common use case for debug logging is to inspect the request or response of the Lambda function. This use case is covered out of the box by the [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) base class.

When debug logging is enabled, the [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) emits the request and response to CloudWatch logs as follows.

**Request Stream:**
```log
*** DEBUG: request stream: { ... }
```

**Response Stream:**
```log
*** DEBUG: response stream: { ... }
```
