![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Namespace

The `Namespace` definition creates a group of nested items. Namespaces are useful for organizing related definitions together.

Nested items are accessed by combining the namespace name and item name with `::`. For example, use `Reporting::Message` to access the variable `Message` in the namespace `Reporting`.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Namespace: String
Description: String
Items:
  - ItemDefinition
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the namespace description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Items</code></dt>
<dd>
The <code>Items</code> section specifies the items defined in the namespace, such as parameters, variables, resources, conditions, mappings, functions, nested modules, resource type definitions, macro definitions, and module imports.

<i>Required:</i> No

<i>Type:</i> List of [Item Definition](Module-Items.md)
</dd>

<dt><code>Namespace</code></dt>
<dd>
The <code>Namespace</code> attribute specifies the item name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>


## Examples

### Organize related resources

```yaml
- Namespace: Accounting
  Description: Resources for accounting capabilities
  Items:

    - Resource: LoggingBucket
      Type: AWS::S3::Bucket

    - Resource: ReportTopic
      Type: AWS::SNS::Topic

- Variable: TopicForAccountingReports
  Value: !Ref Accounting::ReportTopic
```
