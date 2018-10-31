![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Variables - Value Definition

> TODO

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
Variables:
  - ParameterDefinition
```

## Properties

<dl>

<dt><code>Var</code></dt>
<dd>
The <code>Var</code> attribute specifies the variable name.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the variable description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Value</code></dt>
<dd>
The <code>Value</code> attribute specifies the value for the parameter. When used in conjunction with the <code>Resource</code> section, the <code>Value</code> attribute must be a plaintext value and must begin with <code>arn:</code> or be a global wildcard (i.e. <code>*</code>). If no <code>Resource</code> attribute is present, the value can be a CloudFormation expression (e.g. <code>!Ref MyResource</code>).

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

> TODO
