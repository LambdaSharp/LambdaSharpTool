---
title: LambdaSharp.App.Api - LambdaSharp Module
description: Documentation for LambdaSharp.App.Api module
keywords: module, app, api, documentation, overview
---

# Module: LambdaSharp.App.Api
_Version:_ [!include[LAMBDASHARP_VERSION](../version.txt)]


## Overview

The _LambdaSharp.App.Api_ module is used by the `App` declaration to create an API Gateway REST API proxy for CloudWatch Logs, Metrics, and Events that is used by the app. The API can be configured to limit access using the `CoreOrigin` parameter. Access to the API is secured by an API key that is computed from the CloudFormation stack identifier and the app version identifier. Using the `DevMode` parameter, the API can be configured for more lenient access and a simplified API key, which allows for accessing it from _localhost_.

## Resource Types

This module defines no resource types.


## Parameters

<dl>

<dt><code>ParentModuleId</code></dt>
<dd>

The <code>ParentModuleId</code> parameter specifies the module ID of the parent stack.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>ParentModuleInfo</code></dt>
<dd>

The <code>ParentModuleInfo</code> parameter specifies the module information of the parent stack.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>LogGroupName</code></dt>
<dd>

The <code>LogGroupName</code> parameter specifies the name of CloudWatch LogGroup for the app.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>RootPath</code></dt>
<dd>

The <code>RootPath</code> parameter specifies the root path for app API. The root path must be a single path segment.

<i>Required</i>: Yes

<i>Type:</i> String

<i>Pattern:</i> <code>^[a-zA-Z0-9\._\-]+$</code>

</dd>

<dt><code>CorsOrigin</code></dt>
<dd>

The <code>CorsOrigin</code> parameter specifies the source URL that is allowed to invoke the API. The value must be <em>http://</em> or <em>https://</em> followed by a valid domain name in lowercase letters, or <code>*</code> to allow any domain.

<i>Required</i>: Yes

<i>Type:</i> String

<i>Pattern:</i> <code>^\&#42;|https?:\/\/((?!-)[a-z0-9-]{1,63}(?<!-)\.)+[a-z]{2,6}$</code>

</dd>

<dt><code>BurstLimit</code></dt>
<dd>

The <code>BurstLimit</code> parameter specifies the maximum number of requests per second over a short period of time.

<i>Required</i>: Yes

<i>Type:</i> Number

<i>Value Constraints:</i> Minimum value of 10.
</dd>

<dt><code>RateLimit</code></dt>
<dd>

The <code>RateLimit</code> parameter specifies the maximum number of requests per second over a long period of time.

<i>Required</i>: Yes

<i>Type:</i> Number

<i>Value Constraints:</i> Minimum value of 10.
</dd>

<dt><code>EventSource</code></dt>
<dd>

The <code>EventSource</code> parameter specifies the 'Source' property override for app events. When empty, the 'Source' property is set by the app request.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>AppVersionId</code></dt>
<dd>

The <code>AppVersionId</code> parameter specifies the app version identifier. This value is used to construct the complete API key when <code>DevMode</code> is <code>Disabled</code>.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>DevMode</code></dt>
<dd>

The <code>DevMode</code> parameter specifies if the app API should run with relaxed API key constraints and enables debug logging. The value must be one of <code>Enabled</code> or <code>Disabled</code>. Default value is <code>Disabled</code>.

<i>Required</i>: No

<i>Type:</i> String

The <code>DevMode</code> parameter must have one of the following values:
<dl>

<dt><code>Enabled</code></dt>
<dd>

The API key is solely based on the CloudFormation stack identifer. Debug logging is enabled in the app.
</dd>

<dt><code>Disabled</code></dt>
<dd>

The API key is based on teh CloudFormation stack identifier and the app version identifier. Debug logging is disabled in the app.
</dd>

</dl>
</dd>

</dl>


## Output Values
stApi}.execute-api.${AWS::Region}.${AWS::URLSuffix}/${RestApiStage}"


<dl>

<dt><code>ApiKey</code></dt>
<dd>

The <code>ApiKey</code> output contains the CloudFormation stack identifier portion of the API key.

<i>Type:</i> String
</dd>

<dt><code>ApiUrl</code></dt>
<dd>

The <code>ApiUrl</code> output contains the URL of the api endpoint used by the <code>LambdaSharpAppClient</code>.

<i>Type:</i> String
</dd>

<dt><code>DomainName</code></dt>
<dd>

The <code>DomainName</code> output contains the domain name of the API Gateway instance.

<i>Type:</i> String
</dd>

<dt><code>CloudFrontOriginPath</code></dt>
<dd>

The <code>CloudFrontOriginPath</code> output contains the origin path required by a CloudFront distribution to front the API.

<i>Type:</i> String
</dd>

<dt><code>CloudFrontPathPattern</code></dt>
<dd>

The <code>CloudFrontPathPattern</code> output contains the path pattern required by a CloudFront distribution to front the API.

<i>Type:</i> String
</dd>

<dt><code>RootPath</code></dt>
<dd>

The <code>RootPath</code> outputs contains the root path for the app API.

<i>Type:</i> String

</dd>

</dl>


## REST API

The API key is computed by concatenating the app version identifier with the colon character (i.e. `':'`) and the CloudFormation stack identifier, and then applying a base 64 encoding to the result. When `DevMode` is enabled, the API key only uses the CloudFormation stack identifier.

To compute the effective API key, base 64 decode the `ApiKey` output value and concatenate it to the app version identifier:
```javascript
Headers["X-Api-Key"] = Base64Encode($AppVersionId + ':' + Base64Decode($ApiKey))
```

When `DevMode` is enabled, the effective API key becomes:
```javascript
Headers["X-Api-Key"] = $ApiKey
```

By default, the `RootPath` parameter has value `.app`, but this can be customized by the `App` declaration.

### POST:/${RootPath}/logs - Create Log Stream

Creates a new log stream in the app log group. A log stream is a sequence of log events that originate from an app instance.

There is no limit on the number of log streams that can be created. There is a limit of 50 requests-per-second on this operations, after which requests are throttled.

The log stream name must match the following guidelines:
* Log stream names must be unique within the log group.
* Log stream names can be between 1 and 512 characters long.
* The ':' (colon) and '*' (asterisk) characters are not allowed.

#### Request Syntax

```json
{
   "logStreamName": "string"
}
```

#### Request Parameters

The request accepts the following data in JSON format.

<dl>

<dt><code>logStreamName</code></dt>
<dd>

The name of the log stream.

<i>Required:</i> Yes

<i>Type:</i> String

<i>Length Constraints:</i> Minimum length of 1. Maximum length of 512.

<i>Pattern:</i> <code>[^:&#42;]&#42;</code>
</dd>

</dl>

#### Success Response (HTTP Status Code: 200)

On success, the API responds with an empty JSON document.

```json
{ }
```

#### Bad Request Response (HTTP Status Code: 400)

On a _Bad Request_ response, the body contains a message describing why the request was rejected. Additional details can be found in the API logs when enabled.

**Example:** request body is missing required fields
```json
{
    "error": "Invalid request body"
}
```

**Example:** request validation error
```json
{
  "error": "1 validation error detected: Value \'\' at \'logStreamName\' failed to satisfy constraint: Member must have length greater than or equal to 1"
}
```

#### Internal Error Response (HTTP Status Code: 500)

On an _Internal Error_ response, the body contains a generic message. The actual reason can be found in the API logs when enabled.

```json
{
   "error": "Unexpected response from service."
}
```

### PUT:/${RootPath}/logs - Put Log Messages

Uploads a batch of log messages to the specified log stream.

The request must include the sequence token obtained from the response of the previous call, unless it is the first request to a newly created log stream. Using the same `sequenceToken` twice within a narrow time period may cause both calls to be successful or one might be rejected.

The batch of events must satisfy the following constraints:
* The maximum batch size is 1,048,576 bytes. This size is calculated as the sum of all event messages in UTF-8, plus 26 bytes for each log event.
* None of the log events in the batch can be more than 2 hours in the future.
* None of the log events in the batch can be older than 14 days or older than the retention period of the log group.
* The log events in the batch must be in chronological order by their timestamp. The timestamp is the time the event occurred, expressed as the number of milliseconds after Jan 1, 1970 00:00:00 UTC.
* A batch of log events in a single request cannot span more than 24 hours. Otherwise, the operation fails.
* The maximum number of log events in a batch is 10,000.
* There is a quota of 5 requests per second per log stream. Additional requests are throttled. This quota cannot be changed.

#### Request Syntax

```json
{
   "logEvents": [
      {
         "message": "string",
         "timestamp": number
      }
   ],
   "logStreamName": "string",
   "sequenceToken": "string"
}
```

#### Request Parameters

The request accepts the following data in JSON format.

<dl>

<dt><code>logEvents</code></dt>
<dd>

The log events.

<i>Required:</i> Yes

<i>Type:</i> Array of log event objects

<i>Array Members:</i> Minimum number of 1 item. Maximum number of 10,000 items.
<dl>

<dt><code>message</code></dt>
<dd>

The raw event message.

<i>Required:</i> Yes

<i>Type:</i> String

<i>Length Constraints:</i> Minimum length of 1.
</dd>

<dt><code>timestamp</code></dt>
<dd>

The time the event occurred, expressed as the number of milliseconds after Jan 1, 1970 00:00:00 UTC.

<i>Required:</i> Yes

<i>Type:</i> Long

<i>Valid Range:</i> Minimum value of 0.
</dd>

</dl>
</dd>

<dt><code>logStreamName</code></dt>
<dd>

The name of the log stream.

<i>Required:</i> Yes

<i>Type:</i> String

<i>Length Constraints:</i> Minimum length of 1. Maximum length of 512.

<i>Pattern:</i> <code>[^:&#42;]&#42;</code>
</dd>

<dt><code>sequenceToken</code></dt>
<dd>

The sequence token obtained from the response of the previous call. An upload in a newly created log stream does not require a sequence token. Using the same <code>sequenceToken</code> twice within a narrow time period may cause both calls to be successful or one might be rejected.

<i>Type:</i> String
</dd>

</dl>

#### Success Response (HTTP Status Code: 200)

On success, the API responds with a JSON document containing the sequence token for the next request.

```json
{
  "nextSequenceToken": "49608818592289528730168753288679022865213175397425034930"
}
```

#### Bad Request Response (HTTP Status Code: 400)

**Example:** request body is missing required fields
```json
{
    "error": "Invalid request body"
}
```

**Example:** request validation error
```json
{
  "error": "1 validation error detected: Value \'\' at \'logStreamName\' failed to satisfy constraint: Member must have length greater than or equal to 1"
}
```

**Example:** The `sequenceToken` field is either missing or reusing a previous token value
```json
{
  "error": "The given batch of log events has already been accepted. The next batch can be sent with sequenceToken: 49608818592289528730168753288679022865213175397425034930",
  "nextSequenceToken": "49608818592289528730168753288679022865213175397425034930"
}
```

#### Internal Error Response (HTTP Status Code: 500)

On an _Internal Error_ response, the body contains a generic message. The actual reason can be found in the API logs when enabled.

```json
{
   "error": "Unexpected response from service."
}
```

### POST:/${RootPath}/events - Send Events

Sends custom events to the default event bus in Amazon EventBridge so that they can be matched to rules.

#### Request Syntax

```json
{
   "Entries": [
      {
         "Detail": "string",
         "DetailType": "string",
         "Resources": [ "string" ],
         "Source": "string"
      }
   ]
}
```

#### Request Parameters

The request accepts the following data in JSON format.

<dl>

<dt><code>Entries</code></dt>
<dd>

The entry that defines an event in your system. You can specify several parameters for the entry such as the source and type of the event, resources associated with the event, and so on.

<i>Required:</i> Yes

<i>Type:</i> Array of event objects

<i>Length Constraints:</i> Minimum length of 1. Maximum length of 512.

<i>Pattern:</i> <code>[^:&#42;]&#42;</code>

<dl>

<dt><code>Detail</code></dt>
<dd>

A valid JSON string. There is no other schema imposed. The JSON string may contain fields and nested subobjects.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>DetailType</code></dt>
<dd>

Free-form string used to decide what fields to expect in the event detail.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>Resources</code></dt>
<dd>

AWS resources, identified by Amazon Resource Name (ARN), which the event primarily concerns. Any number, including zero, may be present.

<i>Required:</i> Yes

<i>Type:</i> Array of strings
</dd>

<dt><code>Source</code></dt>
<dd>

The source of the event.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

</dl>
</dd>

</dl>

#### Success Response (HTTP Status Code: 200)

On success, the events API responds with an empty JSON document.

```json
{ }
```

#### Internal Error Response (HTTP Status Code: 500)

On an _Internal Error_ response, the body contains a generic message. The actual reason can be found in the API Gateway logs when enabled.

```json
{
   "error": "Unexpected response from service."
}
```