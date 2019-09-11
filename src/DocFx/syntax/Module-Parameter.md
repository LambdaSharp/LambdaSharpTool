# Parameter

The `Parameter` definition specifies a value that is supplied at module deployment time either by the λ# CLI or a parent module. Module parameters can be modified subsequently by updating the CloudFormation stack in the AWS console.

## Syntax

```yaml
Parameter: String
Description: String
Section: String
Label: String
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
Pragmas:
  - PragmaDefinition
Type: String
Allow: AllowDefinition
DefaultAttribute: String
Properties:
  ResourceProperties
EncryptionContext:
  Key-Value Mapping
```

## Properties

<dl>

<dt><code>Allow</code></dt>
<dd>

The <code>Allow</code> attribute can be either a comma-separated, single string value, or a list of string values. String values that contain a colon (<code>:</code>) are interpreted as IAM permission and used as is (e.g. <code>dynamodb:GetItem</code>, <code>s3:GetObject*</code>, etc.). Otherwise, the value is interpreted as a λ# shorthand (see <a href="https://github.com/LambdaSharp/LambdaSharpTool/tree/master/src/LambdaSharp.Tool/Resources/IAM-Mappings.yml">λ# Shorthand by Resource Type</a>). Both notations can be used simultaneously within a single <code>Allow</code> section. Duplicate IAM permissions, after λ# shorthand resolution, are removed.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>AllowedPattern</code></dt>
<dd>

The <code>AllowedPattern</code> attribute specifies a regular expression that represents the patterns to allow for <code>String</code> types.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>AllowedValues</code></dt>
<dd>

The <code>AllowedValues</code> attribute specifies a list of allowed values for the parameter.

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

The <code>Default</code> attribute specifies a value to use when no value is provided for a module deployment. If the parameter defines value constraints, the default value must adhere to those constraints.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>DefaultAttribute</code></dt>
<dd>

The <code>DefaultAttribute</code> attribute specifies the resource attribute to use when exporting the resource from the module or to a Lambda function. By default, the λ# CLI automatically selects the <code>Arn</code> attribute when available. Otherwise, it uses the return value of a <code>!Ref</code> expressions. This behavior can be overwritten by specifying a <code>DefaultAttribute</code> attribute.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the parameter description. The description is shown in the AWS Console when creating or updating the CloudFormation stack.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>EncryptionContext</code></dt>
<dd>

The <code>EncryptionContext</code> section is an optional mapping of key-value pairs used for decrypting a variable of type <code>Secret</code>. For all other types, specifying <code>EncryptionContext</code> will produce a compilation error.

<i>Required</i>: No

<i>Type</i>: Key-Value Pair Mapping
</dd>

<dt><code>Label</code></dt>
<dd>

The <code>Label</code> specifies a human readable label for the parameter. This label is used instead of the parameter name by the AWS Console when updating a CloudFormation stack.

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

The <code>NoEcho</code> attribute specifies whether to mask the parameter value when a call is made that describes the stack. If you set the value to <code>true</code>, the parameter value is masked with asterisks (*****).

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Parameter</code></dt>
<dd>

The <code>Parameter</code> attribute specifies the parameter name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Pragmas</code></dt>
<dd>

The <code>Pragmas</code> section specifies directives that change the default compiler behavior.

<i>Required:</i> No

<i>Type:</i> List of [Pragma Definition](Module-Pragmas.md)
</dd>

<dt><code>Properties</code></dt>
<dd>

The <code>Properties</code> section specifies additional options that can be specified for a managed resource. This section is copied verbatim into the CloudFormation template and can use <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference.html">CloudFormation intrinsic functions</a> (e.g. <code>!Ref</code>, <code>!Join</code>, <code>!Sub</code>, etc.) for referencing other resources.

The <code>Properties</code> section cannot be specified for referenced resources. For a list of all additional options, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: No

<i>Type</i>: Map
</dd>

<dt><code>Scope</code></dt>
<dd>

The <code>Scope</code> attribute specifies which functions need to have access to this item. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the item, then <code>all</code> can be used as a wildcard. In addition, the <code>public</code> can be used to export the item from the module.

<i>Required</i>: No

<i>Type</i>: Comma-delimited String or List of String
</dd>

<dt><code>Section</code></dt>
<dd>

The <code>Section</code> attribute specifies a title for grouping related module inputs together. The AWS Console uses the title for laying out module inputs into sections. The order of the sections and the order of the module inputs in the section is determined by the order in which they occur in the module definition.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> attribute specifies the data type for the parameter. When omitted, the type is assumed to be <code>String</code>.

<i>Required</i>: No

<i>Type</i>: String

The following parameter types are supported by CloudFormation. When using other AWS resource types, λ# automatically declares them as <code>String</code> type.

<dl>

<dt><code>String</code></dt>
<dd>

A literal string.
</dd>

<dt><code>Secret</code></dt>
<dd>

An encrypted string.
</dd>

<dt><code>Number</code></dt>
<dd>

An integer or float. The parameter value is validated as a number. However, when you use the parameter elsewhere in your module (for example, by using the <code>!Ref</code> function), the parameter value becomes a string.
</dd>

<dt><code>List&lt;Number&gt;</code></dt>
<dd>

An array of integers or floats that are separated by commas. The parameter value is validated as numbers. However, when you use the parameter elsewhere in your module (for example, by using the <code>!Ref</code> function), the parameter value becomes a list of strings.
</dd>

<dt><code>CommaDelimitedList</code></dt>
<dd>

An array of literal strings that are separated by commas. The total number of strings should be one more than the total number of commas. Also, each member string is space trimmed.
</dd>

<dt>AWS-Specific Parameter Types</dt>
<dd>

AWS values such as Amazon EC2 key pair names and VPC IDs. For more information, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/parameters-section-structure.html#aws-specific-parameter-types">AWS-Specific Parameter Types</a>.
</dd>

<dt>SSM Parameter Types</dt>
<dd>

Parameters that correspond to existing parameters in Systems Manager Parameter Store. You specify a Systems Manager parameter key as the value of the SSM parameter, and AWS CloudFormation fetches the latest value from Parameter Store to use for the stack. For more information, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/parameters-section-structure.html#aws-ssm-parameter-types">SSM Parameter Types</a>.
</dd>

</dl>
</dd>

</dl>

## Examples

### A parameter

```yaml
- Parameter: MyParameter
  Description: A module parameter
```

### An optional parameter

```yaml
- Parameter: MyParameter
  Description: A module parameter
  Default: no value provided
```

### A parameter with associated IAM permissions

```yaml
- Parameter: MyTopic
  Description: A topic ARN
  Type: AWS::SNS::Topic
  Allow: Publish
```

### An optional parameter that generates a resource on default value

```yaml
- Parameter: MyTopic
  Description: A topic ARN
  Type: AWS::SNS::Topic
  Allow: Publish
  Properties:
    DisplayName: New topic display name
```
