---
title: Mapping Declaration - Module
description: LambdaSharp YAML syntax for CloudFormation mappings
keywords: mapping, declaration, syntax, yaml, cloudformation
---
# Mapping

The `Mapping` definition specifies a section that matches a key to a corresponding set of named values. For example, to set values based on a region, create a mapping that uses the region name as a key and contains the values for each specific region. The [`!FindInMap`](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference-findinmap.html) intrinsic function is used to retrieve values in a map.

## Syntax

```yaml
Mapping: String
Description: String
Value:
  First Level Key-Value Mapping
    Second Level Key-Value Mapping
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the mapping description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Mapping</code></dt>
<dd>

The <code>Mapping</code> attribute specifies the item name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Value</code></dt>
<dd>

The <code>Value</code> attribute specifies the mapping associations. Within the mapping, each map is a key followed by another mapping. The key identifies a map of name-value pairs and must be unique within the mapping. The name can contain only alphanumeric characters (A-Za-z0-9). The keys in mappings must be literal strings. The values can be <code>String</code> or <code>List</code> types.

<i>Required</i>: Yes

<i>Type</i>: Key-Value Pair Mapping
</dd>

</dl>


## Examples

### Basic mapping

```yaml
- Mapping: Greetings
  Description: Time of day greeting
  Value:
    Morning:
      Text: Good morning
    Day:
      Text: Good day
    Evening:
      Text: Good evening
    Night:
      Text: Good night

- Parameter: SelectedTime
  Description: Parameter for selecting the time of day
  AllowedValues:
    - Morning
    - Day
    - Evening
    - Night

- Variable: SelectedGreeting
  Description: Selected greeting
  Value: !FindInMap [ Greetings, !Ref SelectedTime, Text ]
```
