---
title: Import Declaration - Module
description: LambdaSharp YAML syntax for cross-module import references
keywords: cross-module, module, import, declaration, reference, syntax, yaml, cloudformation
---
# Import Definition

The `Import` definition is used to create a cross-module reference. By default, these references are resolved by CloudFormation at deployment time. However, they can also be redirected to a different module or be given a specific value instead. This capability allows for a default behavior that is mostly convenient, while enabling modules to be re-wired to import values from other modules, or to be given specific values for testing or legacy purposes.

## Syntax

```yaml
Import: String
Module: String
Description: String
Scope: ScopeDefinition
Type: String
Allow: AllowDefinition
EncryptionContext:
  Key-Value Mapping
```

## Properties

<dl>

<dt><code>Allow</code></dt>
<dd>

The <code>Allow</code> attribute can be either a comma-separated, single string value, or a list of string values. String values that contain a colon (<code>:</code>) are interpreted as IAM permission and used as is (e.g. <code>dynamodb:GetItem</code>, <code>s3:GetObject*</code>, etc.). Otherwise, the value is interpreted as a LambdaSharp shorthand (see <a href="https://github.com/LambdaSharp/LambdaSharpTool/tree/master/src/LambdaSharp.Tool/Resources/IAM-Mappings.yml">LambdaSharp Shorthand by Resource Type</a>). Both notations can be used simultaneously within a single <code>Allow</code> section. Duplicate IAM permissions, after LambdaSharp shorthand resolution, are removed.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the import parameter description. The description is shown as part of the module's exported values when the <code>Scope</code> includes <code>public</code>.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>EncryptionContext</code></dt>
<dd>

The <code>EncryptionContext</code> section is an optional mapping of key-value pairs used for decrypting a variable of type <code>Secret</code>. For all other types, specifying <code>EncryptionContext</code> will produce a compilation error.

<i>Required</i>: No

<i>Type</i>: Key-Value Pair Mapping
</dd>

<dt><code>Import</code></dt>
<dd>

The <code>Import</code> attribute specifies the import parameter name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Module</code></dt>
<dd>

The <code>Module</code> attribute specifies the name of the module from which to import the value from. The name of imported value can optionally be specified by appending it to the module reference, separated by a double-colon (<code>::</code>). For example, <code>Other.Module::Some::Variable</code> imports the <code>Some::Variable</code> value from the <code>Other.Module</code> module. When omitted, the value of the <code></code> attribute is used instead. Note that the module reference cannot have a version or source bucket specification.

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

The <code>Type</code> attribute identifies the AWS resource type that is being imported. For example, <code>AWS::SNS::Topic</code> declares an SNS topic. For a list of all resource types, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>. When omitted, the type is <code>String</code>. Encrypted values must have type <code>Secret</code> and can optionally specify an <code>EncryptionContext</code> section. These values can be shared as is, or decrypted, when using the <code>::Plaintext</code> suffix on the their full name.

For example, the decrypted value of a variable called <code>Password</code> with type <code>Secret</code> can be accessed by using <code>!Ref Password::Plaintext</code>.

<i>Required</i>: Conditional. The <code>Type</code> attribute is required for new resources and when using the LambdaSharp shorthand notation in the <code>Allow</code> attribute. The <code>Type</code> attribute can be omitted for referenced resources that only list native IAM permissions in their <code>Allow</code> attribute.

<i>Type</i>: String
</dd>

</dl>

## Examples

### Import a module

```yaml
- Import: ImportedMessageTitle
  Module: My.OtherModule::MessageTitle
  Description: Imported title for messages
  Type: String
```

### Import a module output and associate IAM permissions

```yaml
- Import: ImportedTopic
  Module: My.OtherModule::Topic
  Description: Topic ARN for sending notifications
  Type: AWS::SNS::Topic
  Allow: Publish
```
