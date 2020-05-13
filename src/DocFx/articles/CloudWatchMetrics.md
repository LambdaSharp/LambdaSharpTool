---
title: CloudWatch Metrics - Custom CloudWatch Metrics reported by LambdaSharp - LambdaSharp
description: List of custom CloudWatch metrics reported by LambdaSharp modules
keywords: cloudwatch, metrics, modules
---

# CloudWatch Metrics

LambdaSharp modules emit custom CloudWatch metrics to enable automated monitoring on the efficiency and reliability of modules.

LambdaSharp modules can report custom metrics using the built-in [`LogMetric(string name, double value, LambdaMetricUnit unit)`](xref:LambdaSharp.ALambdaFunction.LogMetric(string,double,LambdaMetricUnit)) method or one of its overloads. Metrics are automatically organized by module full name, prefixed by _Module:_. For example, the _LambdaSharp.Core_ metrics are found under the _Module:LambdaSharp.Core_ namespace in CloudWatch.


## Standard Lambda Function Metrics

### Class [LambdaSharp.ALambdaFunction](xref:LambdaSharp.ALambdaFunction)

The `ALambdaFunction` custom metrics are organized by [`Stack`,`Function`] and [`Stack`] dimensions where:
* `Stack` is the CloudFormation stack name.
* `Function` is the Lambda function name.

|Name               |Unit   |Description                                                                        |
|-------------------|-------|-----------------------------------------------------------------------------------|
|LogError.Count     |Count  |Number of error statements logged.                                                 |
|LogFatal.Count     |Count  |Number of fatal error statements logged.                                           |
|LogWarning.Count   |Count  |Number of warning statements logged. This count includes errors logged as warnings.|

#### AWS Metrics

In addition, Lambda emits the following metrics organized by [`FunctionName`], [`Resource`], [`ExecutedVersion`], and [`(none)`] dimension where:
* `FunctionName` is the aggregate metrics for all versions and aliases of a function.
* `Resource` is the metrics for a version or alias of a function.
* `ExecutedVersion` is metrics for a combination of alias and version. Use the `ExecutedVersion` dimension to compare error rates for two versions of a function that are both targets of a weighted alias.
* `(none)` is the aggregate metrics for all functions in the current AWS Region.

|Name                               |Unit   |Description                                                                        |
|-----------------------------------|-------|-----------------------------------------------------------------------------------|
|ConcurrentExecutions                       |Count          |The number of function instances that are processing events. If this number reaches your concurrent executions limit for the Region, or the reserved concurrency limit that you configured on the function, additional invocation requests are throttled.|
|DeadLetterErrors                           |Count          |For asynchronous invocation, the number of times Lambda attempts to send an event to a dead-letter queue but fails. Dead-letter errors can occur due to permissions errors, misconfigured resources, or size limits.|
|DestinationDeliveryFailures                |Count          |For asynchronous invocation, the number of times Lambda attempts to send an event to a destination but fails. Delivery errors can occur due to permissions errors, misconfigured resources, or size limits.|
|Duration                                   |Milliseconds   |The amount of time that your function code spends processing an event. For the first event processed by an instance of your function, this includes initialization time. The billed duration for an invocation is the value of _Duration_ rounded up to the nearest 100 milliseconds.|
|Errors                                     |Count          |The number of invocations that result in a function error. Function errors include exceptions thrown by your code and exceptions thrown by the Lambda runtime. The runtime returns errors for issues such as timeouts and configuration errors. To calculate the error rate, divide the value of Errors by the value of Invocations.|
|Invocations                                |Count          |The number of times your function code is executed, including successful executions and executions that result in a function error. Invocations aren't recorded if the invocation request is throttled or otherwise resulted in an invocation error. This equals the number of requests billed.|
|IteratorAge                                |Milliseconds   |For event source mappings that read from streams, the age of the last record in the event. The age is the amount of time between when the stream receives the record and when the event source mapping sends the event to the function.|
|ProvisionedConcurrencyInvocations          |Count          |The number of times your function code is executed on provisioned concurrency.|
|ProvisionedConcurrencySpilloverInvocations |Count          |The number of times your function code is executed on standard concurrency when all provisioned concurrency is in use.|
|ProvisionedConcurrencyUtilization          |Percent        |For a version or alias, the value of ProvisionedConcurrentExecutions divided by the total amount of provisioned concurrency allocated. For example, .5 indicates that 50 percent of allocated provisioned concurrency is in use.|
|ProvisionedConcurrentExecutions            |Count          |The number of function instances that are processing events on provisioned concurrency. For each invocation of an alias or version with provisioned concurrency, Lambda emits the current count.|
|Throttles                                  |Count          |The number of invocation requests that are throttled. When all function instances are processing requests and no concurrency is available to scale up, Lambda rejects additional requests with `TooManyRequestsException`. Throttled requests and other invocation errors don't count as Invocations or Errors.|
|UnreservedConcurrentExecutions             |Count          |For an AWS Region, the number of events that are being processed by functions that don't have reserved concurrency.|

### Class [LambdaSharp.SimpleQueueService.ALambdaQueueFunction&lt;TMessage&gt;](xref:LambdaSharp.SimpleQueueService.ALambdaQueueFunction`1)

The `ALambdaQueueFunction<TMessage>` custom metrics are organized by [`Stack`,`Function`] and [`Stack`] dimensions where:
* `Stack` is the CloudFormation stack name.
* `Function` is the Lambda function name.

|Name                       |Unit        |Description                                                                       |
|---------------------------|------------|----------------------------------------------------------------------------------|
|MessageFailed.Count        |Count       |Number of messages that failed processing, but have not been forwarded to the dead-letter queue.|
|MessageDead.Count          |Count       |Number of messages that failed processing and have been forwarded to the dead-letter queue.|
|MessageSuccess.Count       |Count       |Number of successfully processed messages.                                        |
|MessageSuccess.Latency     |Milliseconds|Number of milliseconds to successfully process a message once received.           |
|MessageSuccess.Lifespan    |Seconds     |Number of seconds to successfully process a message from the time it was created. |

**NOTE:** The lifespan of a message is determined by first checking for a custom `SentTimestamp` message attribute. If none is found, the built-in SQS `SentTimestamp` message attribute is used instead to determine when the message was created. The custom `SentTimestamp` attribute must a UNIX epoch timestamp in milliseconds stored as a `String` value.

#### AWS Metrics

In addition, SQS emits the following metrics organized by [`QueueName`] dimension where:
* `QueueName` is the Amazon SQS queue name.

|Name                                   |Unit       |Description                                                                    |
|---------------------------------------|-----------|-------------------------------------------------------------------------------|
|ApproximateAgeOfOldestMessage          |Seconds    |The approximate age of the oldest non-deleted message in the queue.            |
|ApproximateNumberOfMessagesDelayed     |Count      |The number of messages in the queue that are delayed and not available for reading immediately.|
|ApproximateNumberOfMessagesNotVisible  |Count      |The number of messages that are in flight.                                     |
|ApproximateNumberOfMessagesVisible     |Count      |The number of messages available for retrieval from the queue.                 |
|NumberOfEmptyReceives                  |Count      |The number of `ReceiveMessage` API calls that did not return a message.        |
|NumberOfMessagesDeleted                |Count      |The number of messages deleted from the queue.                                 |
|NumberOfMessagesReceived               |Count      |The number of messages returned by calls to the ReceiveMessage action.         |
|NumberOfMessagesSent                   |Count      |The number of messages added to a queue.                                       |
|SentMessageSize                        |Bytes      |The size of messages added to a queue.                                         |

For more details, consult the [Amazon SQS metrics documentation](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-available-cloudwatch-metrics.html).

### Class [LambdaSharp.SimpleNotificationService.ALambdaTopicFunction&lt;TMessage&gt;](xref:LambdaSharp.SimpleNotificationService.ALambdaTopicFunction`1)

The `ALambdaTopicFunction<TMessage>` custom metrics are organized by [`Stack`,`Function`] and [`Stack`] dimensions where:
* `Stack` is the CloudFormation stack name.
* `Function` is the Lambda function name.

|Name                       |Unit        |Description                                                                       |
|---------------------------|------------|----------------------------------------------------------------------------------|
|MessageFailed.Count        |Count       |Number of messages that failed processing, but have not been forwarded to the dead-letter queue.|
|MessageDead.Count          |Count       |Number of messages that failed processing and have been forwarded to the dead-letter queue.|
|MessageSuccess.Count       |Count       |Number of successfully processed messages.                                        |
|MessageSuccess.Latency     |Milliseconds|Number of milliseconds to successfully process a message once received.           |
|MessageSuccess.Lifespan    |Seconds     |Number of seconds to successfully process a message from the time it was created. |

**NOTE:** The lifespan of a message is determined by first checking for a custom `SentTimestamp` message attribute. If none is found, the built-in SNS `Timestamp` is used instead to determine when the message was created. The custom `SentTimestamp` attribute must a UNIX epoch timestamp in milliseconds stored as a `String` value.

#### AWS Metrics

In addition, SNS emits the following metrics organized by [`Application`], [`Application`,`Platform`], [`Country`], [`Platform`], [`TopicName`], and [`SMSType`] dimensions where:
* `Application` is the app and device registered with one of the supported push notification services, such as APNs and FCM.
* `Platform` is the platform object for the push notification services, such as APNs and FCM.
* `Country` is the destination country or region of an SMS message.
* `TopicName` is the Amazon SNS topic names.
* `SMSType` is the message type of SMS message.

|Name                                   |Unit       |Description                                                                    |
|---------------------------------------|-----------|-------------------------------------------------------------------------------|
|NumberOfMessagesPublished              |Count      |The number of messages published to your Amazon SNS topics.                    |
|NumberOfNotificationsDelivered         |Count      |The number of messages successfully delivered from your Amazon SNS topics to subscribing endpoints.|
|NumberOfNotificationsFailed            |Count      |The number of messages that Amazon SNS failed to deliver.                      |
|NumberOfNotificationsFilteredOut       |Count      |The number of messages that were rejected by subscription filter policies.     |
|NumberOfNotificationsFilteredOut-InvalidAttributes|Count|The number of messages that were rejected by subscription filter policies because the messages' attributes are invalid.|
|NumberOfNotificationsFilteredOut-NoMessageAttributes|Count|The number of messages that were rejected by subscription filter policies because the messages have no attributes.
|NumberOfNotificationsRedrivenToDlq     |Count      |The number of messages that have been moved to a dead-letter queue.            |
|NumberOfNotificationsFailedToRedriveToDlq|Count    |The number of messages that couldn't be moved to a dead-letter queue.          |
|PublishSize                            |Bytes      |The size of messages published.                                                |
|SMSMonthToDateSpentUSD                 |USD        |The charges you have accrued since the start of the current calendar month for sending SMS messages.|
|SMSSuccessRate                         |Count      |The rate of successful SMS message deliveries.                                 |

For more details, consult the [Amazon SNS metrics documentation](https://docs.aws.amazon.com/sns/latest/dg/sns-monitoring-using-cloudwatch.html).

### Class [LambdaSharp.ApiGateway.ALambdaApiGatewayFunction](xref:LambdaSharp.ApiGateway.ALambdaApiGatewayFunction)

For REST APIs, the `ALambdaApiGatewayFunction` custom metrics are organized by [`Stack`,`Method`,`Resource`] dimension where:
* `Stack` is the CloudFormation stack name.
* `Method` is the HTTP method (e.g. `POST`, `GET`, etc.)
* `Resource` is the API Gateway resource (e.g. `/foo/bar`)

For WebSocket APIs, the `ALambdaApiGatewayFunction` custom metrics are organized by [`Stack`,`Route`] dimension where:
* `Stack` is the CloudFormation stack name.
* `Route` is the WebSocket route key (e.g. `$connect`, `$disconnect`, etc.)

|Name                       |Unit   |Description                                                                    |
|---------------------------|-------|-------------------------------------------------------------------------------|
|AsyncRequestFailed.Count   |Count  |Number of asynchronous messages that failed processing, but have not been forwarded to the dead-letter queue.|
|AsyncRequestDead.Count     |Count  |Number of asynchronous requests that failed and have been forwarded to the dead-letter queue.|
|AsyncRequestSuccess.Count  |Count  |Number of successfully processed asynchronous requests.                        |
|AsyncRequestSuccess.Latency|Milliseconds|Number of milliseconds to successfully process an asynchronous request.             |

#### AWS Metrics

In addition, API Gateway emits the following detailed metrics organized by [`ApiName`,`Method`,`Resource`,`Stage`] dimensions where:
* `ApiName` is the API Gateway name.
* `Method` is the HTTP method (e.g. `POST`, `GET`, etc.)
* `Resource` is the API Gateway resources (e.g. `/foo/bar`)
* `Stage` is the API Gateway sages (i.e. `LATEST`)

|Name                       |Unit        |Description                                                                    |
|---------------------------|------------|-------------------------------------------------------------------------------|
|4XXError                   |Count       |The number of client-side errors captured in a given period.                   |
|5XXError                   |Count       |The number of server-side errors captured in a given period.                   |
|CacheHitCount              |Count       |The number of requests served from the API cache in a given period.            |
|CacheMissCount             |Count       |The number of requests served from the backend in a given period, when API caching is enabled.|
|Count                      |Count       |The total number API requests in a given period.|
|IntegrationLatency         |Milliseconds|The time between when API Gateway relays a request to the backend and when it receives a response from the backend.|
|Latency                    |Milliseconds|The time between when API Gateway receives a request from a client and when it returns a response to the client. The latency includes the integration latency and other API Gateway overhead.|

For more details, consult the [Amazon API Gateway metrics documentation](https://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-metrics-and-dimensions.html).


## Standard Module Metrics

### Module LambdaSharp.Core

Note that Core services must be enabled for _LambdaSharp.Core_ metrics to be reported.

|Name                   |Unit        |Description                                                           |
|-----------------------|------------|----------------------------------------------------------------------|
|ErrorReport.Count      |Count       |Number of errors reported while processing CloudWatch Log events.     |
|LogEvent.Latency       |Milliseconds|Number of milliseconds to to process an ingested CloudWatch Log event.|
|WarningReport.Count    |Count       |Number of warnings reported while processing CloudWatch Log events.   |

## LambdaSharp.Core Metrics Events

When the _Core Services_ are enabled, the emitted LambdaSharp metrics are also published as events to the default event bus on [Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/what-is-amazon-eventbridge.html).

The following is an example of a `LambdaMetrics` event:
```json
{
  "id": "53dc4d37-cffa-4f76-80c9-8b7d4a4d2eaa",
  "detail-type": "LambdaMetrics",
  "source": "LambdaSharp.Core/Logs",
  "account": "123456789012",
  "time": "2019-10-08T16:53:06Z",
  "region": "us-east-1",
  "resources": [
      "lambdasharp:stack:MyTier-Sample-Metric",
      "lambdasharp:module:Sample.Metric",
      "lambdasharp:tier:MyTier"
    ],
  "detail": {
    "_aws": {
        "Timestamp": 1587416172844,
        "CloudWatchMetrics": [
            {
                "Namespace": "Module:Sample.Metric",
                "Dimensions": [
                    [
                        "Stack"
                    ],
                    [
                        "Stack",
                        "Function"
                    ]
                ],
                "Metrics": [
                    {
                        "Name": "CompletedMessages.Latency",
                        "Unit": "Milliseconds"
                    },
                    {
                        "Name": "CompletedMessages.Count",
                        "Unit": "Count"
                    }
                ]
            }
        ]
    },
    "Source": "LambdaMetrics",
    "Version": "2020-04-16",
    "CompletedMessages.Latency": 100,
    "CompletedMessages.Count": 100,
    "Stack": "MyTier-Sample-Metric",
    "Function": "MyFunction"
  }
}
```