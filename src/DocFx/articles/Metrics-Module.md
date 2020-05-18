---
title: LambdaSharp Module Metrics - Custom CloudWatch Metrics reported by LambdaSharp Modules - LambdaSharp
description: List of custom CloudWatch metrics reported by LambdaSharp modules
keywords: cloudwatch, metrics, modules
---

# LambdaSharp Module Metrics

## Module: LambdaSharp.Core

Note that Core services must be enabled for _LambdaSharp.Core_ metrics to be reported.

|Name                       |Unit        |Description                                                           |
|---------------------------|------------|----------------------------------------------------------------------|
|LambdaError.Errors.Count   |Count       |Number of errors reported while processing CloudWatch Log events.     |
|LambdaError.Warnings.Count |Count       |Number of warnings reported while processing CloudWatch Log events.   |
|LogEvent.Latency           |Milliseconds|Number of milliseconds to to process an ingested CloudWatch Log event.|
