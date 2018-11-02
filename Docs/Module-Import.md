![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Import Definition

The `Import` definition is used to create cross-module references. By default, these references are resolved by CloudFormation at deployment time. However, they can also be redirected to a different module output value or be given an specific value instead. This capability makes it possible to have a default behavior that is mostly convenient, while enabling modules to be re-wired to import parameters from other modules, or to be given existing values for testing or legacy purposes.

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

<dl>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the description string to be associated with the import parameter.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Import</code></dt>
<dd>
The <code>Import</code> attribute specifies the cross-module reference using <code>ModuleName::OutputName</code> as notation.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Label</code></dt>
<dd>
The <code>Label</code> specifies a human readable label for the import parameter. This label is used instead of the import parameter name by the AWS Console when updating a CloudFormation stack.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Resource</code></dt>
<dd>
The <code>Resource</code> section specifies the AWS resource type and its IAM access permissions for the import parameter.

<i>Required</i>: No

<i>Type</i>: [Resource Definition](Module-Resource.md)
</dd>

<dt><code>Scope</code></dt>
<dd>
The <code>Scope</code> attribute specifies which functions need to have access to this import parameter. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the import parameter, then <code>"*"</code> can be used as a wildcard.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>Section</code></dt>
<dd>
The <code>Section</code> attribute specifies a title for grouping related module inputs together. The AWS Console uses the title for laying out module inputs into sections. The order of the sections and the order of the module inputs in the section is determined by the order in which they occur in the module definition.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Type</code></dt>
<dd>
The <code>Type</code> attribute specifies the data type for the import parameter. When omitted, the type is assumed to be <code>String</code>.

<i>Required</i>: No

<i>Type</i>: String

The following import parameter types are supported:

<dl>

<dt><code>String</code></dt>
<dd>A literal string.</dd>

<dt><code>Secret</code></dt>
<dd>An encrypted string.</dd>

<dt><code>Number</code></dt>
<dd>An integer or float. The import value is validated as a number. However, when you use the import parameter elsewhere in your module (for example, by using the <code>!Ref</code> function), the import value becomes a string.</dd>

<dt><code>List&lt;Number&gt;</code></dt>
<dd>An array of integers or floats that are separated by commas. The import value is validated as numbers. However, when you use the import parameter elsewhere in your module (for example, by using the <code>!Ref</code> function), the import value becomes a list of strings.</dd>

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

### Import a module output

```yaml
- Import: MyModule::MyOutputTopic
  Description: Topic ARN
```

### Import a module output and associate IAM permissions

```yaml
- Import: MyModule::MyOutputTopic
  Description: Topic ARN
  Resource:
    Type: AWS::SNS::Topic
    Allow: Publish
```
