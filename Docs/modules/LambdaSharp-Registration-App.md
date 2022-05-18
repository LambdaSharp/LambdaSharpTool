---
title: LambdaSharp::Registration::App - LambdaSharp.Core Module
description: Documentation for LambdaSharp::Registration::App resource type
keywords: module, core, documentation, app, registration, resource, type, properties, attributes
---

# LambdaSharp::Registration::App

The `LambdaSharp::Registration::App` type creates a new or updates an existing app registration.

## Using

> **NOTE:** the LambdaSharp CLI automatically adds the required `Using` statement to all modules.

```yaml
Using:

  - Module: LambdaSharp.Core:0.8@lambdasharp
```

## Syntax

```yaml
Type: LambdaSharp::Registration::App
Properties:
  ModuleId: String
  AppId: String
  AppName: String
  AppLogGroupName: String
  AppPlatform: String
  AppFramework: String
  AppLanguage: String
```

## Properties

<dl>

<dt><code>ModuleId</code></dt>
<dd>

The <code>ModuleId</code> property specifies the module CloudFormation stack name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>AppId</code></dt>
<dd>

The <code>AppId</code> property specifies the module app ARN.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>AppName</code></dt>
<dd>

The <code>AppName</code> property specifies the module app name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>AppLogGroupName</code></dt>
<dd>

The <code>AppLogGroupName</code> property specifies the module app CloudWatch log group name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>AppPlatform</code></dt>
<dd>

The <code>AppPlatform</code> property specifies the module app execution platform.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>AppFramework</code></dt>
<dd>

The <code>AppFramework</code> property specifies the module app execution framework.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>AppLanguage</code></dt>
<dd>

The <code>AppLanguage</code> property specifies the module app programming language.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>

## Attributes

<dl>

<dt><code>Registration</code></dt>
<dd>

The <code>Registration</code> attribute contains the app registration ID.

<i>Type</i>: String
</dd>

</dl>
