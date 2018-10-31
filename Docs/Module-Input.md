![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Input Definition

> TODO: description

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Input: String
Section: String
Label: String
Description: String
Scope: ScopeDefinition
NoEcho: Boolean
Default: String
AllowedValues:
  - String
AllowedPattern: String
ConstraintDescription: String
MaxLength: Int
MaxValue: Int
MinLength: Int
MinValue: Int
Resource:
  ResourceDefinition
```

## Properties

> TODO: Input, Section, Label, Scope, Resource

<dl>

<dt><code>AllowedPattern</code></dt>
<dd>
The <code>AllowedPattern</code> attribute specifies a regular expression that represents the patterns to allow for <code>String</code> types.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>AllowedValues</code></dt>
<dd>
The <code>AllowedValues</code> attribute specifies a list of allowed values for the input variable.

<i>Required</i>: No

<i>Type</i>: List of String
</dd>

<dt><code>ConstraintDescription</code></dt>
<dd>
The <code>ConstraintDescription</code> is used to explain a constraint when the constraint is violated.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Default</code></dt>
<dd>
The <code>Default</code> attribute specifies a value to use when no value is provided for a module deployment. If the input variable defines value constraints, the default value must adhere to those constraints.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the input variable description. The description is shown in the AWS Console when creating or updating the CloudFormation stack.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>MaxLength</code></dt>
<dd>
The <code>MaxLength</code> attribute specifies an integer value that determines the largest number of characters you want to allow for <code>String</code> types.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><code>MaxValue</code></dt>
<dd>
The <code>MaxValue</code> attribute specifies a numeric value that determines the largest numeric value you want to allow for <code>Number</code> types.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><code>MinLength</code></dt>
<dd>
The <code>MinLength</code> attribute specifies an integer value that determines the smallest number of characters you want to allow for <code>String</code> types.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><code>MinValue</code></dt>
<dd>
The <code>MinValue</code> attribute specifies a numeric value that determines the smallest numeric value you want to allow for <code>Number</code> types.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><code>NoEcho</code></dt>
<dd>
The <code>NoEcho</code> attribute specifies whether to mask the input variable value when a call is made that describes the stack. If you set the value to <code>true</code>, the input variable value is masked with asterisks (*****).

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
