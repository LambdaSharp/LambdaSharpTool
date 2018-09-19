![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Parameter - Package Section

The `Package` section in a [λ# Module Parameter](ModuleFile-Parameters.md) indicates that local files need to be packaged into a compressed archive that can be deployed. The package definition specifies the local files with a destination S3 bucket and an optional destination key prefix. At build time, the λ# tool creates a package of the local files and automatically copies them to the destination S3 bucket during deployment.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)

## Syntax

```yaml
Files: String
Bucket: String
Prefix: String
```

## Properties

<dl>
<dt><code>Files</code></dt>
<dd>
The <code>Files</code> attribute specifies a path to a local folder. The path can optionally have a wildcard suffix (e.g. <code>*.json</code>). If the wildcard suffix is omitted, all files and sub-folders are included, recursively. Otherwise, only the top folder and files matching the wildcard are included.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Bucket</code></dt>
<dd>
The <code>Bucket</code> attribute specifies the name of a resource parameter of type <code>AWS::S3::Bucket</code> that is the destination for the files.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Prefix</code></dt>
<dd>
The <code>Prefix</code> attribute specifies a key prefix that is prepended to all copied files.

<i>Required</i>: No

<i>Type</i>: String
</dd>
</dl>
