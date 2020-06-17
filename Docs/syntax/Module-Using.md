---
title: Using Section - Module
description: LambdaSharp module Using section
keywords: module, using, section, configuration, syntax, yaml, cloudformation
---
# Using

The `Using` section, in the [LambdaSharp Module](Index.md), lists modules that the current module uses. During deployment, the LambdaSharp CLI checks if the used modules are present. If not, it will attempt to find and deploy them, resolving their dependencies recursively.

## Syntax

```yaml
Module: String
Description: String
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the description for the module dependency.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Module</code></dt>
<dd>

The <code>Module</code> attribute specifies the full name of the required module with an optional version and origin. The format must be <code>Namespace.Name[:Version][@Origin]</code>. Parts in brackets (<code>[ ]</code>) are optional. Without a version specifier, LambdaSharp uses the latest version it can find that is compatible with the LambdaSharp CLI. Without an origin, LambdaSharp uses the deployment bucket name of the active deployment tier as origin. Compilation fails if the LambdaSharp CLI cannot find a published module that matches the criteria.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>

## Examples

### List the `LambdaSharp.S3.IO` module as a dependency

```yaml
Using:

  - Module: LambdaSharp.S3.IO
```

### List a dependency with a specific version number

```yaml
Using:

  - Module: My.OtherModule:2.5
```

### List a dependency with a specific version number and origin bucket

```yaml
Using:

  - Module: My.OtherModule:2.5@SomeBucket
```
