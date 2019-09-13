---
title: Resource Declaration - Module
description: LambdaSharp YAML syntax for generic CloudFormation resources
keywords: resource, declaration, syntax, yaml, cloudformation
---
# Resource

The `Resource` definition is used to create new resources and/or specify access for the Lambda function in the module to existing resources.

## Syntax

```yaml
Resource: String
Description: String
Scope: ScopeDefinition
If: Condition
Type: String
Allow: AllowDefinition
DefaultAttribute: String
Pragmas:
  - PragmaDefinition
Properties:
  ResourceProperties
Value: Expression
DependsOn:
  - String
```

## Properties

<dl>

<dt><code>Allow</code></dt>
<dd>

The <code>Allow</code> attribute can be either a comma-separated, single string value, or a list of string values. String values that contain a colon (<code>:</code>) are interpreted as IAM permission and used as is (e.g. <code>dynamodb:GetItem</code>, <code>s3:GetObject*</code>, etc.). Otherwise, the value is interpreted as a LambdaSharp shorthand (see <a href="https://github.com/LambdaSharp/LambdaSharpTool/tree/master/src/LambdaSharp.Tool/Resources/IAM-Mappings.yml">LambdaSharp Shorthand by Resource Type</a>). Both notations can be used simultaneously within a single <code>Allow</code> section. Duplicate IAM permissions, after LambdaSharp shorthand resolution, are removed.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>DefaultAttribute</code></dt>
<dd>

The <code>DefaultAttribute</code> attribute specifies the resource attribute to use when exporting the resource from the module or to a Lambda function. By default, the LambdaSharp CLI automatically selects the <code>Arn</code> attribute when available. Otherwise, it uses the return value of a <code>!Ref</code> expressions. This behavior can be overwritten by specifying a <code>DefaultAttribute</code> attribute.

<i>Required</i>: No. Not valid when the resource is explicitly referenced by the <code>Value</code> attribute.

<i>Type</i>: String
</dd>

<dt><code>DependsOn</code></dt>
<dd>

The <code>DependsOn</code> attribute identifies items that must be created prior. For additional information, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-dependson.html">CloudFormation DependsOn Attribute</a>.

<i>Required</i>: No. Not valid when the resource is explicitly referenced by the <code>Value</code> attribute.

<i>Type</i>: List of String
</dd>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the variable description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>If</code></dt>
<dd>

The <code>If</code> attribute specifies a condition that must be met for the Lambda function to be included in the deployment. The condition can either the name of a <code>Condition</code> item or a logical expression.

<i>Required</i>: No. Not valid when the resource is explicitly referenced by the <code>Value</code> attribute.

<i>Type</i>: String or Expression
</dd>

<dt><code>Pragmas</code></dt>
<dd>

The <code>Pragmas</code> section specifies directives that change the default compiler behavior.

<i>Required:</i> No. Not valid when the resource is explicitly referenced by the <code>Value</code> attribute.

<i>Type:</i> List of [Pragma Definition](Module-Pragmas.md)
</dd>

<dt><code>Properties</code></dt>
<dd>

The <code>Properties</code> section specifies additional options that can be specified for a new resource. This section is copied verbatim into the CloudFormation template and can use <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference.html">CloudFormation intrinsic functions</a> (e.g. <code>!Ref</code>, <code>!Join</code>, <code>!Sub</code>, etc.) for referencing other resources.

The <code>Properties</code> section cannot be specified for referenced resources. For a list of all additional options, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: No. Not valid when the resource is explicitly referenced by the <code>Value</code> attribute.

<i>Type</i>: Map
</dd>

<dt><code>Resource</code></dt>
<dd>

The <code>Resource</code> attribute specifies the item name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Scope</code></dt>
<dd>

The <code>Scope</code> attribute specifies which functions need to have access to this item. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the item, then <code>all</code> can be used as a wildcard. In addition, the <code>public</code> can be used to export the item from the module.

<i>Required</i>: No

<i>Type</i>: Comma-delimited String or List of String
</dd>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> attribute identifies the AWS resource type that is being created or referenced. For example, <code>AWS::SNS::Topic</code> declares an SNS topic. For a list of all resource types, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: Conditional. The <code>Type</code> attribute is required for new resources and when using the LambdaSharp shorthand notation in the <code>Allow</code> attribute. The <code>Type</code> attribute can be omitted for referenced resources that only list native IAM permissions in their <code>Allow</code> attribute.

<i>Type</i>: String
</dd>

<dt><code>Value</code></dt>
<dd>

The <code>Value</code> attribute specifies the value for the parameter. If the <code>Value</code> attribute is a list of resource names, the IAM permissions are requested for all of them.

<i>Required</i>: Conditional. The <code>Value</code> attribute is required for referenced resources. Otherwise, it must be omitted.

<i>Type</i>: String
</dd>

</dl>

## Examples

### Create an SNS topic

```yaml
- Resource: MyTopic
  Type: AWS::SNS::Topic
  Allow: Publish
```

### Create a DynamoDB Table

```yaml
- Resource: MyDynamoDBTable
  Scope: all
  Type: AWS::DynamoDB::Table
  Allow: Subscribe
  Properties:
    BillingMode: PAY_PER_REQUEST
    AttributeDefinitions:
      - AttributeName: MessageId
        AttributeType: S
    KeySchema:
      - AttributeName: MessageId
        KeyType: HASH
```

### Create a DynamoDB Table configured by module parameters

```yaml
- Parameter: DynamoReadCapacity
  Type: Number
  Default: 1

- Parameter: DynamoWriteCapacity
  Type: Number
  Default: 1

- Resource: MyDynamoDBTable
  Scope: all
  Type: AWS::DynamoDB::Table
  Allow: Subscribe
  Properties:
    AttributeDefinitions:
      - AttributeName: MessageId
        AttributeType: S
    KeySchema:
      - AttributeName: MessageId
        KeyType: HASH
    ProvisionedThroughput:
      ReadCapacityUnits: !Ref DynamoReadCapacity
      WriteCapacityUnits: !Ref DynamoWriteCapacity
```

### Request full access to all S3 buckets

```yaml
- Resource: GrantBucketAccess
  Type: AWS::S3::Bucket
  Allow: Full
  Value:
    - arn:aws:s3:::*
    - arn:aws:s3:::*/*
```

### Request access to AWS Rekognition

```yaml
- Resource: RekognitionService
  Description: Permissions required for using AWS Rekognition
  Value: "*"
  Allow:
    - "rekognition:DetectFaces"
    - "rekognition:IndexFaces"
    - "rekognition:SearchFacesByImage"
```
