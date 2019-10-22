---
title: Variable Declaration - Module
description: LambdaSharp YAML syntax for module variables
keywords: variable, declaration, syntax, yaml, cloudformation
---
# Variable

The `Variable` definition specifies a literal value or an expression. Variables are inlined during compilation. They can be used by other variables, resources, and functions. Variables, like resources, can be scoped to functions or to be public.

## Syntax

```yaml
Variable: String
Description: String
Type: String
Scope: ScopeDefinition
Value: Expression
EncryptionContext:
  Key-Value Mapping
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

The <code>EncryptionContext</code> section is an optional mapping of key-value pairs used for decrypting a variable of type <code>Secret</code>. For all other types, specifying <code>EncryptionContext</code> will produce a compilation error.

<i>Required</i>: No

<i>Type</i>: Key-Value Pair Mapping
</dd>

<dt><code>Scope</code></dt>
<dd>

The <code>Scope</code> attribute specifies which functions need to have access to this item. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the item, then <code>all</code> can be used as a wildcard. In addition, the <code>public</code> can be used to export the item from the module.

<i>Required</i>: No

<i>Type</i>: Comma-delimited String or List of String
</dd>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> attribute specifies the variable type. When omitted, the type is <code>String</code>. Encrypted values must have type <code>Secret</code> and can optionally specify an <code>EncryptionContext</code> section. These values can be shared as is, or decrypted, when using the <code>::Plaintext</code> suffix on the their full name.

For example, the decrypted value of a variable called <code>Password</code> with type <code>Secret</code> can be accessed by using <code>!Ref Password::Plaintext</code>.
</dd>

<dt><code>Value</code></dt>
<dd>

The <code>Value</code> attribute specifies the value for the variable. The <code>Value</code> can be a literal value, an expression, or a list of expressions.

<i>Required</i>: Yes

<i>Type</i>: Expression
</dd>

<dt><code>Variable</code></dt>
<dd>

The <code>Variable</code> attribute specifies the item name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>

## Return Values

### Ref

When the logical ID of this item is provided to the `Ref` intrinsic function, `Ref` returns the value of the variable. If the value of the variable is a list, the expressions in the list are concatenated into a comma-separated string.

## Examples

### Literal value

```yaml
- Variable: MyValue
  Value: Hello World!
```

### List of literal values

```yaml
- Variable: MyList
  Value:
    - First
    - Second
```

### Expression value

```yaml
- Variable: MyExpression
  Value: !Sub "${AWS::StackName}-${AWS::Region}"
```

### List of expressions

```yaml
- Variable: MyExpressionList
  Value:
    - !Sub "${AWS::StackName}-${Module::FullName}"
    - !Sub "${AWS::StackName}-${AWS::Region}"
```


