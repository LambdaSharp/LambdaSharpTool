---
title: LambdaSharp.App.EventBus - LambdaSharp Module
description: Documentation for LambdaSharp.App.EventBus module
keywords: module, app, event bus, documentation, overview
---

# Module: LambdaSharp.App.EventBus
_Version:_ [!include[LAMBDASHARP_VERSION](../version.txt)]

## Overview

The _LambdaSharp.App.EventBus_ module is used by the `App` declaration to create an API Gateway WebSocket proxy for CloudWatch EventBridge. The proxy is only created if the app has at least one event source. When created, the proxy manages event pattern subscriptions to forwarded events. The proxy uses the same notation as CloudWatch EventBridge to describe event patterns. This design promotes a unified way to work with CloudWatch events both in the backend and frontend. Access to the EventBus API is secured by an API key that is computed from the CloudFormation stack identifier and the app version identifier. Using the `DevMode` parameter, the EventBus API can be configured for more lenient access and a simplified API key, which allows for accessing it from _localhost_.

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

<dl>

<dt><code>ApiKey</code></dt>
<dd>

The <code>ApiKey</code> output contains the CloudFormation stack identifier portion of the API key.

<i>Type:</i> String
</dd>

<dt><code>Url</code></dt>
<dd>

The <code>Url</code> output contains the URL of the api endpoint used by the <code>LambdaSharpEventBusClient</code>.

<i>Type:</i> String
</dd>

</dl>

