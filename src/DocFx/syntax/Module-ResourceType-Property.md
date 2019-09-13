---
title: Property Declaration - ResourceType
description: LambdaSharp YAML syntax for custom resource type properties
keywords: custom resource, resource type, property, declaration, syntax, yaml, cloudformation
---
# Property Definition

The property definition specifies the name and type for a resource type property.

## Syntax

```yaml
Name: String
Description: String
Type: String
Required: Boolean
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the description of the resource type property.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Name</code></dt>
<dd>

The <code>Name</code> attribute specifies the name of the property. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Required</code></dt>
<dd>

The <code>Required</code> attribute specifies if the property must be provided. When omitted, the value is assumed to be <code>true</code>.

<i>Required</i>: No

<i>Type</i>: Boolean
</dd>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> attribute specifies the data type for the property. When omitted, the type is assumed to be <code>String</code>.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>