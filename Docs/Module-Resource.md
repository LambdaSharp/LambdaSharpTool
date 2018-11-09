![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Resource Section

The `Resource` section specifies the resource type and requested IAM permissions.

Access permissions can be specified using the IAM `Allow` notation or the λ# shorthand notation for supported resources. See the [λ# Shorthand by Resource Type](../src/MindTouch.LambdaSharp.Tool/Resources/IAM-Mappings.yml) YAML file for up-to-date support.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)

## Syntax

```yaml
Type: String
Allow: AllowDefinition
Properties:
  ResourceProperties
DependsOn:
  - String
ArnAttribute: String
```

## Properties

<dl>

<dt><code>Allow</code></dt>
<dd>
The <code>Allow</code> attribute can either a comma-separated, single string value or a list of string values. String values that contain a colon (<code>:</code>) are interpreted as IAM permission and used as is (e.g. <code>dynamodb:GetItem</code>, <code>s3:GetObject*</code>, etc.). Otherwise, the value is interpreted as a λ# shorthand (see <a href="../src/MindTouch.LambdaSharp.Tool/Resources/IAM-Mappings.yml">λ# Shorthand by Resource Type</a>). Both notations can be used simultaneously within a single <code>Allow</code> section. Duplicate IAM permissions, after λ# shorthand resolution, are removed.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>ArnAttribute</code></dt>
<dd>
The <code>ArnAttribute</code> attribute specifies the name of the resource attribute to use in a <code>!GetAtt</code> expression to obtain the resource ARN. By default, the λ# CLI automatically maps known AWS resource types to the appropriate ARN attribute name. However, if no such mapping is defined, the λ# CLI generates a <code>!Ref</code> expression instead. The ARN is required for associating IAM permissions with resource.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>DependsOn</code></dt>
<dd>
The <code>DependsOn</code> attribute identifies resources that must be created in a specific order. Variable name must match another Variable name within the module that is generating a resource. For additional information, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-dependson.html">CloudFormation DependsOn Attribute</a>.

<i>Required</i>: No

<i>Type</i>: List of String (VariableNames)
</dd>

<dt><code>Properties</code></dt>
<dd>
The <code>Properties</code> section specifies additional options that can be specified for a managed resource. This section is copied verbatim into the CloudFormation template and can use <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference.html">CloudFormation intrinsic functions</a> (e.g. <code>!Ref</code>, <code>!Join</code>, <code>!Sub</code>, etc.) for referencing other resources.

The <code>Properties</code> section cannot be specified for referenced resources. For a list of all additional options, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: No

<i>Type</i>: Map
</dd>

<dt><code>Type</code></dt>
<dd>
The <code>Type</code> attribute identifies the AWS resource type that is being declared. For example, <code>AWS::SNS::Topic</code> declares an SNS topic. For a list of all resource types, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: Conditional. The <code>Type</code> attribute is required for new resources and when using the λ# shorthand notation in the <code>Allow</code> attribute. The <code>Type</code> attribute can be omitted for referenced resources that only list native IAM permissions in their <code>Allow</code> attribute.

<i>Type</i>: String
</dd>

</dl>
