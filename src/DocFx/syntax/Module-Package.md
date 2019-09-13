---
title: Package Declaration - Module
description: LambdaSharp YAML syntax for artifact packages
keywords: artifact, package, declaration, zip, executable, elf, syntax, yaml, cloudformation
---
# Package

The package definition creates a compressed zip package from a local path. The zip package is then uploaded to the deployment S3 bucket during the LambdaSharp CLI publish step. All items in the zip package are given read-write permissions, unless the item has a Linux executable with an [ELF header](https://en.wikipedia.org/wiki/Executable_and_Linkable_Format), in which case the item is given read-and-execute permission (see [GifMaker Sample](https://github.com/LambdaSharp/GifMaker-Sample)).

## Syntax

```yaml
Package: String
Description: String
Scope: ScopeDefinition
Files: String
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the package description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Files</code></dt>
<dd>

The <code>Files</code> attribute specifies a path to a local folder. The path can optionally have a wildcard suffix (e.g. <code>*.json</code>). If the wildcard suffix is omitted, all files and sub-folders are included, recursively. Otherwise, only the top folder and files matching the wildcard are included.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Package</code></dt>
<dd>

The <code>Package</code> attribute specifies the item name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Scope</code></dt>
<dd>

The <code>Scope</code> attribute specifies which functions need to have access to this item. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the item, then <code>all</code> can be used as a wildcard. In addition, the <code>public</code> can be used to export the item from the module.

<i>Required</i>: No

<i>Type</i>: Comma-delimited String or List of String
</dd>

</dl>

## Return Values

### Ref

When the logical ID of this item is provided to the `Ref` intrinsic function, `Ref` returns the S3 key of the published zip package.

## Examples

### Create package of local files

```yaml
- Package: MyPackage
  Files: WebAssets/
```
