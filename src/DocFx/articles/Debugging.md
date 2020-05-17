---
title: Debug Logging - Lambda Functions - LambdaSharp
description: Description of how to enable debug logging for Lambda functions
keywords: debug, logs, cloudwatch
---

# Debug Logging

Lambda functions deployed by LambdaSharp have a `DEBUG_LOGGING_ENABLED` environment variable to enable debug logging after deployment. By default, `DEBUG_LOGGING_ENABLED` is set to `false`.

## Custom Debug Logging

At runtime, the value of the `DEBUG_LOGGING_ENABLED` environment variable can be checked through the [`DebugLoggingEnabled`](xref:LambdaSharp.ALambdaFunction.DebugLoggingEnabled) property.

Debug statements are logged by using [LogDebug(string format, params object[] arguments)](xref:LambdaSharp.ALambdaFunction.LogDebug(System.String,Sytem.Object[])) and appear in CloudWatch logs with a `*** DEBUG:` prefix:
```
*** DEBUG: this entry is only emitted when debug logging is enabled
```

For complex debug statements, it is best practice to first check the `DebugLoggingEnabled` property before invoking `LogDebug(...)`. Checking if debug logging is enabled first avoids incurring wasteful overhead when the debug statement will be discarded anyway.

```csharp
if(DebugLoggingEnabled) {
    LogDebug(CreateComplexDebugStatement());
}
```

## Request/Response Payload Logging

The most common use case for debug logging is to inspect the request or response payload of the Lambda function. This use case is covered out of the box by the [ALambdaFunction](xref:LambdaSharp.ALambdaFunction) base class.

When debug logging is enabled, the `ALambdaFunction` emits the request and response payloads in CloudWatch logs as follows.

**Request Stream:**
```
*** DEBUG: request stream: { ... }
```

**Response Stream:**
```
*** DEBUG: response response: { ... }
```

## Enable Debug Logging

To enable debug logging, follow these steps:
1. Go to the AWS Console
1. Locate the Lambda function
1. Click _Edit_ next to the environment variables section
1. Change the value for `DEBUG_LOGGING_ENABLED` to `true`
1. Click _Save_.

This operation will restart the Lambda function with debug logging enabled.
