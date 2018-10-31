![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Variables - Secret Definition

> TODO: description

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Var: String
Description: String
Scope: ScopeDefinition
Secret: String
EncryptionContext:
  Key-Value Mapping
Variables:
  - ParameterDefinition
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the variable description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>EncryptionContext</code></dt>
<dd>
The <code>EncryptionContext</code> section is an optional mapping of key-value pairs used for decrypting the <code>Secret</code> value.

<i>Required</i>: No

<i>Type</i>: Key-Value Pair Mapping
</dd>

<dt><code>Scope</code></dt>
<dd>
The <code>Scope</code> attribute specifies which functions need to have access to this import parameter. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the import parameter, then <code>"*"</code> can be used as a wildcard.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>Secret</code></dt>
<dd>
The <code>Secret</code> attribute specifies an encrypted value that is decrypted at runtime by the Lambda function. Note that the required decryption key must either be specified in the <code>Secrets</code> section or be passed in using the <code>ModuleSecrets</code> input parameter to grant <code>kms:Decrypt</code> to module IAM role.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Var</code></dt>
<dd>
The <code>Var</code> attribute specifies the variable name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Variables</code></dt>
<dd>
The <code>Variables</code> section contains a collection of nested variables. To reference a nested variable, combine the parent variable and nested variables names with a double-colon (e.g. <code>Parent::NestedVariable</code>).

<i>Required:</i> No

<i>Type:</i> List of [Variable Definition](Module-Variables.md)
</dd>

</dl>

## Examples

> TODO: examples
