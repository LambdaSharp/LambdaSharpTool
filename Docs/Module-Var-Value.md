![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Literal Value Variable

A literal value variable is useful for capturing values thar are reused in multiple places, or for creating composite values from other variables or module parameters. Instead of hard-coded values, it is advisable to use [module parameters](Module-Parameter.md) with default value instead.

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

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the variable description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Scope</code></dt>
<dd>
The <code>Scope</code> attribute specifies which functions need to have access to this import parameter. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the import parameter, then <code>"*"</code> can be used as a wildcard.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>Value</code></dt>
<dd>
The <code>Value</code> attribute specifies the value for the parameter. The <code>Value</code> can be a single value or a list of values. Lists of values are concatenated into a comma-separated list when being passed to a Lambda function.

<i>Required</i>: Yes

<i>Type</i>: String or List Expression
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

### Literal value

```yaml
- Var: MyValue
  Value: Hello World!
```

### List of values

```yaml
- Var: MyList
  Value:
    - First
    - Second
```

### Expression value

```yaml
- Var: MyExpression
  Value: !Sub "${AWS::StackName}-${AWS::Region}"
```
