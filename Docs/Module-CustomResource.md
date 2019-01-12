![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Custom Resource Output

The `CustomResource` definition is used to register a custom resource handler for a deployment tier. The handler can either be an SNS topic or Lambda function. Once deployed, the custom resource is available to all subsequent module deployments.

A deployed module with a custom resource handler that is in-use by another deployed module cannot be torn down or changed. All dependent modules must first be torn down before the custom resource handler can be modified or deleted.

The custom resource handler is invoked by using its definition name as resource type. For example,

```yaml
- Var: MyResource
  Resource:
    Type: MyCustom::ResourceName
    Properties:
      # ...
```

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)
* [Notes](#notes)

## Syntax

```yaml
CustomResource: String
Description: String
Handler: String
```

## Properties

<dl>

<dt><code>CustomResource</code></dt>
<dd>
The <code>CustomResource</code> attribute specifies the name of the custom resource type. Custom resource types are globally defined per deployment tier. The name must contain a double-colon (<code>::</code>), such as <code>ModuleName::ResourceType</code>.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the description of the module's output variable.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Handler</code></dt>
<dd>
The <code>Handler</code> attribute specifies the name of an SNS topic parameter, variable, or function.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>

## Examples

### Custom Resource using an SNS Topic as handler

```yaml
- CustomResource: Accounting::Report
  Description: Custom resource for creating accounting reports
  Handler: AccountReportGeneratorTopic

# ...

- Var: AccountReportGeneratorTopic
  Resource:
    Type: AWS::SNS::Topic
    Allow: Publish

# ...

- Function: AccountReportGenerator
  Memory: 128
  Timeout: 30
  Sources:
    - Topic: AccountReportGeneratorTopic
```

### Custom Resource using a Lambda function

```yaml
- CustomResource: Accounting::Report
  Description: Custom resource for creating accounting reports
  Handler: AccountReportGenerator

# ...

- Function: AccountReportGenerator
  Memory: 128
  Timeout: 30
  Sources:
    - Topic: AccountReportGeneratorTopic
```

## Notes

A custom resource handler is translated into a [CloudFormation export](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-stack-exports.html) value, which is automatically tracked. The AWS console and CLI can show all CloudFormation stacks that depend on the custom resource. As long as any stack uses it, CloudFormation will prevent the exported value from being modified or deleted. Modifying a custom resource handler therefore requires all dependent stacks to be first deleted.

This protection mechanism is of great benefit, because custom resource failures are some of the most tedious failures to deal with in CloudFormation. Inadvertently modifying a custom resource handler would impact all stacks that dependent on it, making updating impossible and causing failure on delete.
