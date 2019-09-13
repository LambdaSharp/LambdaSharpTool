---
title: ResourceType Declaration - Module
description: LambdaSharp YAML syntax for custom resource types
keywords: custom resource, resource type, lambda, declaration, syntax, yaml, cloudformation
---
# Resource Type

The `ResourceType` definition is used to register a new resource type for a deployment tier. The handler for the resource type can either be an SNS topic or a Lambda function. Once deployed, the resource type is available to all subsequent module deployments.

A deployed module with a resource type that is in-use by another deployed module cannot be torn down or changed. All dependent modules must first be removed before the resource type can be modified or deleted.

## Syntax

```yaml
ResourceType: String
Description: String
Handler: String
Properties:
  - PropertyDefinition
Attributes:
  - AttributeDefinition
```

## Properties

<dl>

<dt><code>Attributes</code></dt>
<dd>

The <code>Attributes</code> section specifies the attributes returned by the resource type. The LambdaSharp CLI uses this information to validate access to attributes on a resource of this resource type.

<i>Required</i>: Yes

<i>Type</i>: List of [Attribute Definition](Module-ResourceType-Attribute.md)
</dd>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the description of the exported resource type handler.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Handler</code></dt>
<dd>

The <code>Handler</code> attribute specifies the name of an SNS topic or Lambda function.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>ResourceType</code></dt>
<dd>

The <code>ResourceType</code> attribute specifies the name of the resource type. Resource types are globally defined per deployment tier. The name must contain a double-colon (<code>::</code>), such as <code>ModuleName::ResourceType</code>.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Properties</code></dt>
<dd>

The <code>Properties</code> section specifies the properties required by the resource type. The LambdaSharp CLI uses this information to validate the initialization of a resource using this resource type.

<i>Required</i>: Yes

<i>Type</i>: List of [Property Definition](Module-ResourceType-Property.md)
</dd>

</dl>

## Examples

### Resource type using an SNS Topic as handler

```yaml
- ResourceType: Accounting::Report
  Description: Resource type for creating accounting reports
  Handler: AccountReportGeneratorTopic
  Properties:
    - Name: Name
      Description: Name of report to create
      Type: String
      Required: true
  Attributes:
    - Name: Url
      Description: Location of created report
      Type: String

- Resource: AccountReportGeneratorTopic
  Type: AWS::SNS::Topic
  Allow: Subscribe

- Function: AccountReportGenerator
  Memory: 128
  Timeout: 30
  Sources:
    - Topic: AccountReportGeneratorTopic
```

### Resource type using a Lambda function

```yaml
- ResourceType: Accounting::Report
  Description: Resource type for creating accounting reports
  Handler: AccountReportGenerator
  Properties:
    - Name: Name
      Description: Name of report to create
      Type: String
      Required: true
  Attributes:
    - Name: Url
      Description: Location of created report
      Type: String

- Function: AccountReportGenerator
  Memory: 128
  Timeout: 30
  Sources:
    - Topic: AccountReportGeneratorTopic
```

### Using a resource type

```yaml
- Resource: MyReport
  Type: Accounting::Report
  Properties:
    Name: MyNewReport

- Variable: MyCreatedReportUrl
  Scope: public
  Value: !GetAtt MyReport.Url
```

## Notes

The resource type handler is translated into a [CloudFormation export](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-stack-exports.html) value, which is automatically tracked. The AWS console and CLI can show all CloudFormation stacks that depend on the custom resource type handler. As long as any stack uses it, CloudFormation will prevent the exported value from being modified or deleted. Modifying a custom resource handler therefore requires all dependent stacks to be deleted first.

This protection mechanism is of great benefit, because custom resource failures are some of the most tedious failures to deal with in CloudFormation. Inadvertently modifying a custom resource handler would impact all stacks that depended on it, making updating impossible and causing failures on delete.
