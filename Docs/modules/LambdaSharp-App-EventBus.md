---
title: LambdaSharp.App.EventBus - LambdaSharp Module
description: Documentation for LambdaSharp.App.EventBus module
keywords: module, app, event bus, documentation, overview
---

# Module: LambdaSharp.App.EventBus
_Version:_ [!include[LAMBDASHARP_VERSION](../version.txt)]

## Overview

The _LambdaSharp.App.EventBus_ module is used by the `App` declaration to create an API Gateway WebSocket proxy for EventBridge. The proxy is only created if the app has at least one event source. When created, the proxy manages event pattern subscriptions to forwarded events. The proxy uses the same notation as EventBridge to describe event patterns. This design promotes a unified way to work with events both in the backend and frontend. Access to the EventBus API is secured by an API key that is computed from the CloudFormation stack identifier and the app version identifier. Using the `DevMode` parameter, the EventBus API can be configured for more lenient access and a simplified API key, which allows for accessing it from _localhost_.

## Resource Types

This module defines no resource types.


## Parameters

<dl>

<dt><code>AppVersionId</code></dt>
<dd>

The <code>AppVersionId</code> parameter specifies the app version identifier. This value is used to construct the complete API key.

<i>Required</i>: Yes

<i>Type:</i> String
</dd>

<dt><code>DevMode</code></dt>
<dd>

The <code>DevMode</code> parameter specifies if the app EventBus API should run with relaxed API key constraints and enables debug logging. The value must be one of <code>Enabled</code> or <code>Disabled</code>. Default value is <code>Disabled</code>.

<i>Required</i>: No

<i>Type:</i> String

The <code>DevMode</code> parameter must have one of the following values:
<dl>

<dt><code>Disabled</code></dt>
<dd>

The API key is based on teh CloudFormation stack identifier and the app version identifier. Debug logging is disabled in the app.
</dd>

</dl>
</dd>

<dt><code>Enabled</code></dt>
<dd>

The API key is solely based on the CloudFormation stack identifer. Debug logging is enabled in the app.
</dd>

</dl>


## Output Values

<dl>

<dt><code>ApiKey</code></dt>
<dd>

The <code>ApiKey</code> output contains the CloudFormation stack identifier portion of the API key.

<i>Type:</i> String
</dd>

<dt><code>EventTopicArn</code></dt>
<dd>

The <code>EventTopicArn</code> output contains the SNS topic ARN for broadcasting events to the app event bus.

<i>Type:</i> AWS::SNS::Topic
</dd>

<dt><code>Url</code></dt>
<dd>

The <code>Url</code> output contains the URL of the api endpoint used by the <code>LambdaSharpEventBusClient</code>.

<i>Type:</i> String
</dd>

<dt><code>WebSocketApiId</code></dt>
<dd>

The <code>WebSocketApiId</code> output contains the ID of the WebSocket API.

<i>Type:</i> AWS::ApiGatewayV2::Api
</dd>

</dl>

## WebSocket API

The WebSocket API endpoint can be found in the `Url` output value of the nested stack. The WebSocket API manages the broadcasting of subscribed EventBridge events to clients. Each client can have an arbitrary number of subscription rules using the [EventBridge pattern](https://docs.aws.amazon.com/eventbridge/latest/userguide/filtering-examples-structure.html) notation.

Note that events must first be published to WebSocket SNS topic before they can be subscribed to. The ARN of the SNS topic can be found in the `EventTopicArn` output value of the nested stack.

### Authentication

The WebSocket API expects a `header` query parameter in the request to validate access to the API. The value of the `header` query parameter is a base64 encoded JSON document.

#### Header Document

```json
{
    "Host": "string",
    "ApiKey": "string",
    "Id": "guid"
}
```

#### Header Properties

<dl>

<dt><code>Host</code></dt>
<dd>

The WebSocket domain name.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>ApiKey</code></dt>
<dd>

The API key to authorize access to the WebSocket. The API key is computed by concatenating <code>AppVersionId</code> with the colon character (i.e. <code>':'</code>) and the CloudFormation stack identifier, and then applying a base 64 encoding to the result. When <code>DevMode</code> is enabled, the API key only uses the CloudFormation stack identifier.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>Id</code></dt>
<dd>

The app instance ID or session ID. Must be a unique GUID.

<i>Required:</i> Yes

<i>Type:</i> GUID
</dd>

</dl>

#### Sample Header Encoding

The following example illustrates how to encode the JSON header into a query parameter.

**Original header document:**
```json
{
    "Host": "acme.execute-api.us-west-2.amazonaws.com",
    "ApiKey": "ZWU4OTc0MjAtODgzNi0xMWViLWFmMmMtMDIxZTQ5Njc5YTBi",
    "Id": "4a114560-fa5b-4f94-a462-69fbaf432b86"
}
```

**Encoded header value:**
```
wss://acme.execute-api.us-west-2.amazonaws.com/LATEST/?header=ewogICAgIkhvc3QiOiAiYWNtZS5leGVjdXRlLWFwaS51cy13ZXN0LTIuYW1hem9uYXdzLmNvbSIsCiAgICAiQXBpS2V5IjogIlpXVTRPVGMwTWpBdE9EZ3pOaTB4TVdWaUxXRm1NbU10TURJeFpUUTVOamM1WVRCaSIsCiAgICAiSWQiOiAiNGExMTQ1NjAtZmE1Yi00Zjk0LWE0NjItNjlmYmFmNDMyYjg2Igp9Cg==
```

### Protocol

The WebSocket protocol has two types of interactions: actions and notifications. _Actions_ are similar to requests in that each action has exactly one response, called acknowledgements. _Acknowledgements_ are correlated to actions by the `RequestId` property. _Notifications_ can occur at any time and are commonly  triggered by a subscription rule.

### Action Syntax

Each request must have a `Action` property with a known value. The `Action` property indicates the shape of the request. In addition, must have a `RequestId` property to correlate responses from the WebSocket with their requests.

```json
{
    "Action": "string",
    "RequestId": "guid"
}
```

#### Properties

The following properties are required for all requests.

<dl>

<dt><code>Action</code></dt>
<dd>

The name of the action to invoke.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>RequestId</code></dt>
<dd>

The unique identifier for the request.

<i>Required:</i> Yes

<i>Type:</i> GUID
</dd>

</dl>

#### Acknowledge: Success

A response to an action has `Ack` as its `Action` value. The `RequestId` must be used to correlate a response to an earlier request. The order in which responses are sent back is non-deterministic and the client must be able to associate responses correctly to pending requests.

A successful acknowledge response has `Ok` as its `Status` value.

```json
{
    "Action": "Ack",
    "RequestId": "guid",
    "Status": "Ok"
}
```

<dl>

<dt><code>Action</code></dt>
<dd>

The value is always <code>Ack</code>.

<i>Type:</i> String
</dd>

<dt><code>RequestId</code></dt>
<dd>

The unique identifier for the request correlating it to a matching <em>Action</em>.

<i>Type:</i> GUID
</dd>

<dt><code>Status</code></dt>
<dd>

The value is always <code>Ok</code>.

<i>Type:</i> String
</dd>

</dl>


#### Acknowledge: Error

A response to an action has `Ack` as its `Action` value. The `RequestId` must be used to correlate a response to an earlier request. The order in which responses are sent back is non-deterministic and the client must be able to associate responses correctly to pending requests.

A failed acknowledge response has `Error` as its `Status` value. In addition, the `Message` property contains a description of the error.

```json
{
    "Action": "Ack",
    "RequestId": "guid",
    "Status": "Error",
    "Message": "string"
}
```

<dl>

<dt><code>Action</code></dt>
<dd>

The value is always <code>Ack</code>.

<i>Type:</i> String
</dd>

<dt><code>Message</code></dt>
<dd>

A description of the error that occurred.

<i>Type:</i> String
</dd>

<dt><code>RequestId</code></dt>
<dd>

The unique identifier for the request correlating it to a matching <em>Action</em>.

<i>Type:</i> GUID
</dd>

<dt><code>Status</code></dt>
<dd>

The value is always <code>Error</code>.

<i>Type:</i> String
</dd>

</dl>

### Action: Hello

The `Hello` action must be sent by the client as soon as the connection is opened. Failure to send the `Hello` action will cause the connection to be automatically closed by the WebSocket API.

```json
{
    "Action": "Hello",
    "RequestId": "guid"
}
```

### Action: Subscribe

The `Subscribe` action is used to create or update a subscription rule. The subscription rule has an <a href="https://docs.aws.amazon.com/eventbridge/latest/userguide/filtering-examples-structure.html">EventBridge pattern</a> to describe which events should be sent to the client.

Note that only events published to the `EventTopicArn` SNS topic can be subscribed to.

```json
{
    "Action": "Subscribe",
    "RequestId": "guid",
    "Rule": "string",
    "Pattern": "pattern"
}
```

#### Properties

The following properties are in addition to the default request properties.

<dl>

<dt><code>Rule</code></dt>
<dd>

The name of the subscription rule to create or update.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>Pattern</code></dt>
<dd>

The <a href="https://docs.aws.amazon.com/eventbridge/latest/userguide/filtering-examples-structure.html">EventBridge pattern</a> describing the events to subscribe to.

Note that only events published to the <code>EventTopicArn</code> SNS topic can be subscribed to.

<i>Required:</i> Yes

<i>Type:</i> <a href="https://docs.aws.amazon.com/eventbridge/latest/userguide/filtering-examples-structure.html">EventBridge pattern</a> as a JSON string
</dd>

</dl>

### Action: Unsubscribe

The `Unsubscribe` action is used to delete a subscription rule.

```json
{
    "Action": "Unsubscribe",
    "RequestId": "guid",
    "Rule": "string"
}
```

#### Properties

The following properties are in addition to the default request properties.

<dl>

<dt><code>Rule</code></dt>
<dd>

The name of the subscription rule to delete.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

</dl>

### Notification: Event

An event notification has `Event` as its `Action` value. The WebSocket API sends this notification when an EventBridge event published to `EventTopicArn` matches one or more subscription rules.

```json
{
  "Action": "Event",
  "Rules": [ "string" ],
  "Source": "string",
  "Type": "string",
  "Event": "json",
  "RequestId": "guid"
}
```

#### Properties

<dl>

<dt><code>Action</code></dt>
<dd>

The value is always <code>Event</code>.

<i>Type:</i> String
</dd>

<dt><code>Event</code></dt>
<dd>

The <a href="https://docs.aws.amazon.com/eventbridge/latest/userguide/aws-events.html">EventBridge event</a> payload that matched the subscription rule.

<i>Type:</i> <a href="https://docs.aws.amazon.com/eventbridge/latest/userguide/aws-events.html">EventBridge event</a> as a JSON string
</dd>

<dt><code>RequestId</code></dt>
<dd>

The unique identifier for the request.

<i>Type:</i> GUID
</dd>

<dt><code>Rules</code></dt>
<dd>

A list of subscription rules that were matched by the event.

<i>Type:</i> List of String
</dd>

<dt><code>Source</code></dt>
<dd>

The value of the <code>source</code> property from the EventBridge event.

<i>Type:</i> String
</dd>

<dt><code>Type</code></dt>
<dd>

The value of the <code>detail-type</code> property from the EventBridge event.

<i>Type:</i> String
</dd>

</dl>

**Sample EventBridge event:**
```json
{
  "version": "0",
  "id": "6a7e8feb-b491-4cf7-a9f1-bf3703467718",
  "detail-type": "EC2 Instance State-change Notification",
  "source": "aws.ec2",
  "account": "111122223333",
  "time": "2017-12-22T18:43:48Z",
  "region": "us-west-1",
  "resources": [
    "arn:aws:ec2:us-west-1:123456789012:instance/i-1234567890abcdef0"
  ],
  "detail": {
    "instance-id": "i-1234567890abcdef0",
    "state": "terminated"
  }
}
```

### Notification: KeepAlive

A keep-alive notification has `KeepAlive` as its `Action` value. The WebSocket API sends this notification periodically to keep the WebSocket connection alive.

```json
{
  "Action": "KeepAlive",
  "RequestId": "guid"
}
```

<dl>

<dt><code>Action</code></dt>
<dd>

The value is always <code>KeepAlive</code>.

<i>Type:</i> String
</dd>

<dt><code>RequestId</code></dt>
<dd>

The unique identifier for the request.

<i>Type:</i> GUID
</dd>

</dl>
