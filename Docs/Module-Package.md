![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Package Variable

The package variable specifies a local path to be compressed into a package. The compressed package is then uploaded with other module assets during the publishing process. The compressed package is then deployed to a specified bucket when the module is deployed.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Package: String
Description: String
Scope: ScopeDefinition
Files: String
Bucket: String
Prefix: String
Variables:
  - VariableDefinition
```

## Properties

<dl>

<dt><code>Bucket</code></dt>
<dd>
The <code>Bucket</code> attribute specifies the name of a resource parameter of type <code>AWS::S3::Bucket</code> that is the destination for the files.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the variable description.

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
The <code>Package</code> attribute specifies a variable name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Prefix</code></dt>
<dd>
The <code>Prefix</code> attribute specifies a key prefix that is prepended to all copied files.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Scope</code></dt>
<dd>
The <code>Scope</code> attribute specifies which functions need to have access to this import parameter. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the import parameter, then <code>"*"</code> can be used as a wildcard.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>Variables</code></dt>
<dd>
The <code>Variables</code> section contains a collection of nested variables. To reference a nested variable, combine the parent variable and nested variables names with a double-colon (e.g. <code>Parent::NestedVariable</code>).

<i>Required:</i> No

<i>Type:</i> List of [Variable Definition](Module-Variables.md)
</dd>

</dl>

## Examples


### Create package of local files

```yaml
- Package: MyPackage
  Files: WebAssets
  Bucket: MyBucket
  Prefix: assets/
```
