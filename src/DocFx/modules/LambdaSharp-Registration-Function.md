---
title: LambdaSharp::Registration::Function - LambdaSharp.Core Module
description: Documentation for LambdaSharp::Registration::Function resource type
keywords: module, core, documentation, function, registration, resource, type, properties, attributes
---

# LambdaSharp::Registration::Function

The `LambdaSharp::Registration::Function` type creates a new or updates an existing function registration.

## Using

> **NOTE:** the LambdaSharp CLI automatically adds the required `Using` statement to all modules.

```yaml
Using:

  - Module: LambdaSharp.Core:0.5
```

## Syntax

```yaml
Type: LambdaSharp::Registration::Function
Properties:
  ModuleId: String
  FunctionId: String
  FunctionName: String
  FunctionLogGroupName: String
  FunctionMaxMemory: String
  FunctionMaxDuration: String
  FunctionPlatform: String
  FunctionFramework: String
  FunctionLanguage: String
```

## Properties

<dl>

<dt><code>ModuleId</code></dt>
<dd>

The <code>ModuleId</code> property specifies the module CloudFormation stack name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>FunctionId</code></dt>
<dd>

The <code>FunctionId</code> property specifies the module function ARN.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>FunctionName</code></dt>
<dd>

The <code>FunctionName</code> property specifies the module function name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>FunctionLogGroupName</code></dt>
<dd>

The <code>FunctionLogGroupName</code> property specifies the module function CloudWatch log group name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>FunctionMaxMemory</code></dt>
<dd>

The <code>FunctionMaxMemory</code> property specifies the max memory for module function.

<i>Required</i>: Yes

<i>Type</i>: Number
</dd>

<dt><code>FunctionMaxDuration</code></dt>
<dd>

The <code>FunctionMaxDuration</code> property specifies the max duration for module function.

<i>Required</i>: Yes

<i>Type</i>: Number
</dd>

<dt><code>FunctionPlatform</code></dt>
<dd>

The <code>FunctionPlatform</code> property specifies the module function execution platform.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>FunctionFramework</code></dt>
<dd>

The <code>FunctionFramework</code> property specifies the module function execution framework.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>FunctionLanguage</code></dt>
<dd>

The <code>FunctionLanguage</code> property specifies the module function programming language.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>

## Attributes

<dl>

<dt><code>Registration</code></dt>
<dd>

The <code>Registration</code> attribute contains the function registration ID.

<i>Type</i>: String
</dd>

</dl>
