![λ#](../../../Docs/LambdaSharpLogo.png)

# LambdaSharp::Registration::Module

The `LambdaSharp::Registration::Module` type creates a new or updates an existing module registration. The registration is used to combine the logs from the module functions into a single module report.

__Topics__
* [Using](#using)
* [Syntax](#syntax)
* [Properties](#properties)
* [Attributes](#attributes)

## Using

> **NOTE:** the λ# CLI automatically adds the required `Using` statement to all modules.

```yaml
Using:

  - Module: LambdaSharp.Core:0.5
```

## Syntax

```yaml
Type: LambdaSharp::Registration::Module
Properties:
  ModuleId: String
  Module: String
```

## Properties

<dl>

<dt><code>ModuleId</code></dt>
<dd>
The <code>ModuleId</code> property specifies the module CloudFormation stack name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Module</code></dt>
<dd>
The <code>Module</code> property specifies the module full name and version.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>

## Attributes

<dl>

<dt><code>Registration</code></dt>
<dd>
The <code>Registration</code> attribute contains the module registration ID.

<i>Type</i>: String
</dd>

</dl>
