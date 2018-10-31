![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Import Definition

> TODO: description

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Import: String
Section: String
Label: String
Description: String
Scope: ScopeDefinition
NoEcho: Boolean
Resource:
  ResourceDefinition
```

## Properties

> TODO: Import, Section, Label, Scope, Resource

<dl>

<dt><code>Description</code></dt>
<dd>
A string of up to 4000 characters that describes the input variable.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Type</code></dt>
<dd>
The <code>Type</code> attribute specifies the data type for the input variable. When omitted, the type is assumed to be <code>String</code>.

<i>Required</i>: No

<i>Type</i>: String

The following input variable types are supported:

<dl>

<dt><code>String</code></dt>
<dd>A literal string.</dd>

<dt><code>Secret</code></dt>
<dd>An encrypted string.</dd>

<dt><code>Number</code></dt>
<dd>An integer or float. The input value is validated as a number. However, when you use the input variable elsewhere in your module (for example, by using the <code>!Ref</code> function), the input value becomes a string.</dd>

<dt><code>List&lt;Number&gt;</code></dt>
<dd>An array of integers or floats that are separated by commas. The input value is validated as numbers. However, when you use the input variable elsewhere in your module (for example, by using the <code>!Ref</code> function), the input value becomes a list of strings.</dd>

<dt><code>CommaDelimitedList</code></dt>
<dd>An array of literal strings that are separated by commas. The total number of strings should be one more than the total number of commas. Also, each member string is space trimmed.</dd>

<dt>AWS-Specific Parameter Types</dt>
<dd>AWS values such as Amazon EC2 key pair names and VPC IDs. For more information, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/parameters-section-structure.html#aws-specific-parameter-types">AWS-Specific Parameter Types</a>.</dd>

<dt>SSM Parameter Types</dt>
<dd>Parameters that correspond to existing parameters in Systems Manager Parameter Store. You specify a Systems Manager parameter key as the value of the SSM parameter, and AWS CloudFormation fetches the latest value from Parameter Store to use for the stack. For more information, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/parameters-section-structure.html#aws-ssm-parameter-types">SSM Parameter Types</a>.</dd>

</dl>
</dd>





</dl>


## Examples

> TODO: add examples
