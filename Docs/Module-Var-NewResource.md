![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - New Resource Variable

A new resource variable instantiates a CloudFormation resource using the `Type` attribute and settings of the optional `Properties` section.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Var: String
Description: String
Scope: ScopeDefinition
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
The <code>Resource</code> section specifies the AWS resource type and its IAM access permissions for the variable. The resource definition is used to create new resource and associate with the variable.

<i>Required</i>: Yes

<i>Type</i>: [Resource Definition](Module-Resource.md)
</dd>

<dt><code>Scope</code></dt>
<dd>
The <code>Scope</code> attribute specifies which functions need to have access to this import parameter. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the import parameter, then <code>"*"</code> can be used as a wildcard.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
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

### Create an SNS topic

```yaml
- Var: MyTopic
  Resource:
    Type: AWS::SNS::Topic
    Allow: Publish
```

### Create a DynamoDB Table

```yaml
- Var: MyDynamoDBTable
  Scope: "*"
  Resource:
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
        ReadCapacityUnits: 1
        WriteCapacityUnits: 1
```

### Create a DynamoDB Table configured by module parameters

```yaml
- Parameter: DynamoReadCapacity
  Type: Number
  Default: 1

- Parameter: DynamoWriteCapacity
  Type: Number
  Default: 1

# ...

- Var: MyDynamoDBTable
  Scope: "*"
  Resource:
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
