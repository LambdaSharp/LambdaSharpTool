---
title: Condition Declaration - Module
description: LambdaSharp YAML syntax for CloudFormation conditions
keywords: condition, declaration, syntax, yaml, cloudformation
---
# Condition

The `Condition` definition specifies a statement that defines the circumstances under which entities are created or configured.

## Syntax

```yaml
Condition: String
Description: String
Value: Expression
```

## Properties

<dl>

<dt><code>Condition</code></dt>
<dd>

The <code>Condition</code> attribute specifies the item name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the condition description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Value</code></dt>
<dd>

The <code>Value</code> attribute specifies a boolean expression using the intrinsic functions <code>!And</code>, <code>!Equals</code>, <code>!Not</code>, and <code>!Or</code>. The <code>!Ref</code> function is used to access module parameters. The <code>!Condition</code> function is used to reference other conditions.

<i>Required</i>: Yes

<i>Type</i>: Key-Value Pair Mapping
</dd>

</dl>


## Examples

### Conditional resources

```yaml
- Parameter: EnvType
  Description: Environment type.
  Default: test
  Type: String
  AllowedValues:
    - prod
    - test
  ConstraintDescription: must specify prod or test.

- Resource: EC2Instance
  Type: "AWS::EC2::Instance"
  Properties:
    ImageId: ami-0ff8a91507f77f867

- Group: ProductionResources
  Items:

    - Condition: Create
      Value: !Equals [ !Ref EnvType, prod ]

    - Resource: MountPoint
      Type: AWS::EC2::VolumeAttachment
      If: ProductionResources::Create
      Properties:
        InstanceId: !Ref EC2Instance
        VolumeId: !Ref ProductionResources::NewVolume
        Device: /dev/sdh

    - Resource: NewVolume
      Type: AWS::EC2::Volume
      If: ProductionResources::Create
      Properties:
        Size: 100
        AvailabilityZone: !GetAtt EC2Instance.AvailabilityZone
```
