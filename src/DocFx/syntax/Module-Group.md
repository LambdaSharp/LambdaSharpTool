---
title: Group Declaration - Module
description: LambdaSharp YAML syntax for module item groups
keywords: module, group, declaration, syntax, yaml, cloudformation
---
# Group

The `Group` definition creates a group of nested items. Groups are useful for organizing related definitions together.

Nested items are accessed by combining the group name and item name with `::`. For example, use `Reporting::Message` to access the variable `Message` in the group `Reporting`.

## Syntax

```yaml
Group: String
Description: String
Items:
  - ItemDefinition
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the group description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Items</code></dt>
<dd>

The <code>Items</code> section specifies the items defined in the group, such as parameters, variables, resources, conditions, mappings, functions, nested modules, resource type definitions, macro definitions, and module imports.

<i>Required:</i> No

<i>Type:</i> List of [Item Definition](Module-Items.md)
</dd>

<dt><code>Group</code></dt>
<dd>

The <code>Group</code> attribute specifies the item name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

</dl>


## Examples

### Organize related resources

```yaml
- Group: Accounting
  Description: Resources for accounting capabilities
  Items:

    - Resource: LoggingBucket
      Type: AWS::S3::Bucket

    - Resource: ReportTopic
      Type: AWS::SNS::Topic

- Variable: TopicForAccountingReports
  Value: !Ref Accounting::ReportTopic
```
