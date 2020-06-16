---
title: Macro Declaration - Module
description: LambdaSharp YAML syntax for CloudFormation macros
keywords: macro, lambda, declaration, syntax, yaml, cloudformation
---
# Macro

The `Macro` definition registers a [CloudFormation Macro](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/template-macros.html) for the deployment tier. The handler must be a Lambda function. Once deployed, the macro is available to all subsequent module deployments.

**WARNING:** Due to a limitation in the `Fn::Transform` function, it is not possible to namespace macros to a deployment tier. Consequently, macros are defined globally for the entire account, across all deployment tiers.

## Syntax

```yaml
Macro: String
Description: String
Handler: String
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the description of the module's output variable.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Handler</code></dt>
<dd>

The <code>Handler</code> attribute specifies the name of a function.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>

<dt><code>Macro</code></dt>
<dd>

The <code>Macro</code> attribute specifies the name of the macro. Macros are globally defined per deployment tier.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

## Examples

### Multiple macro definition using the same Lambda function

```yaml
- Macro: ToUpper
  Description: CloudFormation macro for converting a string to uppercase
  Handler: StringOpFunction

- Macro: ToLower
  Description: CloudFormation macro for converting a string to uppercase
  Handler: StringOpFunction

- Function: StringOpFunction
  Memory: 128
  Timeout: 15
```
