![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Variables - Referenced Resource Definition

> TODO: description
In addition, variables can be associated to resources, which will grant the module IAM role the requested permissions.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Var: String
Description: String
Scope: ScopeDefinition
Value: String
Resource:
  ResourceDefinition
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

<dt><code>Resource</code></dt>
<dd>
The <code>Resource</code> section specifies the AWS resource type and its IAM access permissions for the resource referenced by the <code>Value</code> attribute.

<i>Required</i>: Yes

<i>Type</i>: [Resource Definition](Module-Resource.md)
</dd>

<dt><code>Scope</code></dt>
<dd>
The <code>Scope</code> attribute specifies which functions need to have access to this import parameter. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the import parameter, then <code>"*"</code> can be used as a wildcard.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>Value</code></dt>
<dd>
The <code>Value</code> attribute specifies the value for the parameter. When used in conjunction with the <code>Resource</code> section, the <code>Value</code> attribute must be a plaintext value and must begin with <code>arn:</code> or be a global wildcard (i.e. <code>*</code>). If no <code>Resource</code> attribute is present, the value can be a CloudFormation expression (e.g. <code>!Ref MyResource</code>).

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
