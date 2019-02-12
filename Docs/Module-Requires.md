![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Using

The `Using` section, in the [λ# Module](Module.md), lists modules that the current module uses. During deployment, the λ# CLI checks if the used modules are present. If not, it will attempt to find and deploy them, resolving their dependencies recursively.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

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
The <code>Module</code> attribute specifies the full name of the required module, its version, and origin. The format of the source must be <code>Owner.Name[:Version][@BucketName]</code>. Parts in brackets (<code>[]</code>) are optional. Without a version specifier, λ# uses the latest version it can find. Without an origin bucket name, λ# uses the configured deployment bucket. Compilation fails if the λ# CLI cannot find a published module that matches the criteria.

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
