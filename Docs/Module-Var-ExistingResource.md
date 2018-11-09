![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Referenced Resource Variable

A referenced resource variable is used to grant access to a pattern of resources. No new resources are created when a reference resource variable is defined.

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
Resource:
  ResourceDefinition
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

<dt><code>Resource</code></dt>
<dd>
The <code>Resource</code> section specifies the AWS resource type and its IAM access permissions for the resource referenced by the <code>Value</code> attribute.

<i>Required</i>: Yes

<i>Type</i>: [Resource Definition](Module-Resource.md)
</dd>

<dt><code>Scope</code></dt>
<dd>
The <code>Scope</code> attribute specifies which functions need to have access to this import parameter. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the import parameter, then <code>"*"</code> can be used as a wildcard.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>Value</code></dt>
<dd>
The <code>Value</code> attribute specifies the value for the parameter. If the <code>Value</code> attribute is a list of resource names, the IAM permissions are requested for all of them.

<i>Required</i>: Yes

<i>Type</i>: String
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

### Request full access to all S3 buckets

```yaml
- Var: GrantBucketAccess
  Value:
    - arn:aws:s3:::*
    - arn:aws:s3:::*/*
  Resource:
    Type: AWS::S3::Bucket
    Allow: Full
```

### Request access to AWS Rekognition

```yaml
- Var: RekognitionService
  Description: Permissions required for using AWS Rekognition
  Value: "*"
  Resource:
    Allow:
      - "rekognition:DetectFaces"
      - "rekognition:IndexFaces"
      - "rekognition:SearchFacesByImage"
```
