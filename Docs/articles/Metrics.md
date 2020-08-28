---
title: LambdaSharp Metrics - Custom CloudWatch Metrics reported by LambdaSharp - LambdaSharp
description: List of custom CloudWatch metrics reported by LambdaSharp modules
keywords: cloudwatch, metrics, functions, modules
---

# LambdaSharp Metrics

LambdaSharp modules emit custom CloudWatch metrics to enable automated monitoring on the efficiency and reliability of modules.

LambdaSharp modules can report custom metrics using the built-in [`LogMetric(string name, double value, LambdaMetricUnit unit)`](xref:LambdaSharp.ALambdaFunction.LogMetric(System.String,System.Double,LambdaSharp.Logging.Metrics.LambdaMetricUnit)) method or one of its overloads. Metrics are automatically organized by module full name, prefixed by _Module:_. For example, the _LambdaSharp.Core_ metrics are found under the _Module:LambdaSharp.Core_ namespace in CloudWatch.

## Related
* [Function Metrics](Metrics-Function.md) describes the custom CloudWatch metrics emitted by LambdaSharp base classes.
* [Module Metrics](Metrics-Module.md) describes the custom CloudWatch metrics emitted by LambdaSharp modules.