![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Parameter - Resource Section

The presence of the `Resource` section in a [λ# Module Parameter](ModuleFile-Parameters.md) indicates that the parameter corresponds to an AWS resource. Similarly to regular parameter values, the resource values (e.g. ARN, Queue URL, etc.) are passed to the Lambda functions, so that they can retrieved during function initialization.

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
```

## Properties

<dl>
<dt><code>Type</code></dt>
<dd>
The <code>Type</code> attribute identifies the AWS resource type that is being declared. For example, <code>AWS::SNS::Topic</code> declares an SNS topic. For a list of all resource types, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Allow</code></dt>
<dd>
The <code>Allow</code> attribute can either a comma-separated, single string value or a list of string values. String values that contain a colon (<code>:</code>) are interpreted as IAM permission and used as is (e.g. <code>dynamodb:GetItem</code>, <code>s3:GetObject*</code>, etc.). Otherwise, the value is interpreted as a λ# shorthand (see <a href="../src/MindTouch.LambdaSharp.Tool/Resources/IAM-Mappings.yml">λ# Shorthand by Resource Type</a>). Both notations can be used simultaneously within a single <code>Allow</code> section. Duplicate IAM permissions, after λ# shorthand resolution, are removed. Resource parameters without the <code>Allow</code> attribute are omitted from the Lambda function configuration.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>Properties</code></dt>
<dd>
The <code>Properties</code> section specifies additional options that can be specified for a managed resource. This section is copied verbatim into the CloudFormation template and can use <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference.html">CloudFormation intrinsic functions</a> (e.g. <code>!Ref</code>, <code>!Join</code>, <code>!Sub</code>, etc.) for referencing other resources.

The <code>Properties</code> section cannot be specified for referenced resources. For a list of all additional options, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: No

<i>Type</i>: Map
</dd>
</dl>

<dt><code>DependsOn</code></dt>
<dd>
The <code>DependsOn</code> attribute identifies resources that must be created in a specific order. Parameter name must match another parameter name within the module that is generating a resource. For additional information, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-dependson.html">CloudFormation DependsOn Attribute</a>. 

<i>Required</i>: No

<i>Type</i>: List of String (ParameterNames)
</dd>
</dl>
