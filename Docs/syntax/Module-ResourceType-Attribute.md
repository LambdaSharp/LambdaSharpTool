---
title: Attribute Declaration - ResourceType
description: LambdaSharp YAML syntax for custom resource type attributes
keywords: custom resource, resource type, attribute, declaration, syntax, yaml, cloudformation
---
# Attribute Definition

The attribute definition specifies the name and type for a resource type attribute.

## Syntax

```yaml
Name: String
Description: String
Type: String
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the description of the resource type attribute.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Name</code></dt>
<dd>

The <code>Name</code> attribute specifies the name of the attribute. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> attribute specifies the data type for the attribute. When omitted, the type is assumed to be <code>String</code>.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>