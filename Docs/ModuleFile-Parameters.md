![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Parameters Section

The `Parameters` section, in the [λ# Module](ModuleFile.md), defines parameters that are made available to every Lambda deployed function in the module. Parameters can be defined inline as plaintext, as secrets, imported from the [AWS Parameter Store](https://aws.amazon.com/systems-manager/features/#Parameter_Store), or generated dynamically. In addition, parameters can be associated to resources, which will grant the module IAM role the requested permissions. Finally, parameters can also be exported to the Parameter Store where they can be read by other applications.

Parameters must have a `Name` and may have a `Description`. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

The computed values are passed into the Lambda functions as environment variables, so they can be retrieved during execution.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)

## Syntax

```yaml
Name: String
Description: String
Value: String
Values:
  - String
Secret: String
EncryptionContext:
  Key-Value Mapping
Import: String
Package:
  PackageDefinition
Export: String
Resource:
  ResourceDefinition
Parameters:
  - ParameterDefinition
```

## Properties

<dl>
<dt><code>Name</code></dt>
<dd>
The <code>Name</code> attribute specifies the parameter name used by Lambda functions to retrieve the parameter value.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the parameter description used by the AWS Systems Manager Parameter Store for exported parameter values.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Value</code></dt>
<dd>
The <code>Value</code> attribute specifies the value for the parameter. When used in conjunction with the <code>Resource</code> section, the <code>Value</code> attribute must be a plaintext value and must begin with <code>arn:</code> or be a global wildcard (i.e. <code>*</code>). If no <code>Resource</code> attribute is present, the value can be a CloudFormation expression (e.g. <code>!Ref MyResource</code>).

<i>Required</i>: No. At most one <code>Value</code>, <code>Values</code>, <code>Secret</code>, <code>Import</code>, or <code>Package</code> can be specified at a time.

<i>Type</i>: String
</dd>

<dt><code>Values</code></dt>
<dd>
The <code>Values</code> section is a list of plaintext values that are concatenated into a single parameter value using commas (<code>,</code>).

The <code>Values</code> section cannot be used in conjunction with the <code>Resource</code> section.

<i>Required</i>: No. At most one <code>Value</code>, <code>Values</code>, <code>Secret</code>, <code>Import</code>, or <code>Package</code> can be specified at a time.

<i>Type</i>: List of String
</dd>

<dt><code>Secret</code></dt>
<dd>
The <code>Secret</code> attribute specifies an encrypted value that is decrypted at runtime by the Lambda function. Note that the required decryption key must be specified in the <code>Secrets</code> section to grant <code>kms:Decrypt</code> to module IAM role.

The <code>Secret</code> attribute cannot be used in conjunction with a <code>Resource</code> section or <code>Export</code> attribute.

<i>Required</i>: No. At most one <code>Value</code>, <code>Values</code>, <code>Secret</code>, <code>Import</code>, or <code>Package</code> can be specified at a time.

<i>Type</i>: String
</dd>

<dt><code>EncryptionContext</code></dt>
<dd>
The <code>EncryptionContext</code> section is an optional mapping of key-value pairs used for decrypting the <code>Secret</code> value.

<i>Required</i>: No. Can only be used in conjunction with <code>Secret</code>.

<i>Type</i>: Key-Value Pair Mapping
</dd>

<dt><code>Import</code></dt>
<dd>
The <code>Import</code> attribute specifies a path to the AWS Systems Manager Parameter Store. At build time, the λ# tool imports the value and stores it in a Lambda function environment variable. If the value starts with <code>/</code>, it will be used as an absolute key path. Otherwise, it will be prefixed with <code>/{{Tier}}/</code> to create an import path specific to the deployment tier.

<i>Required</i>: No. At most one <code>Value</code>, <code>Values</code>, <code>Secret</code>, or <code>Import</code> can be specified at a time.

<i>Type</i>: String
</dd>

<dt><code>Package</code></dt>
<dd>
The <code>Package</code> section specifies local files with a destination S3 bucket and an optional destination key prefix. At build time, the λ# tool creates a package of the local files and automatically copies them to the destination S3 bucket during deployment.

<i>Required</i>: No. At most one <code>Value</code>, <code>Values</code>, <code>Secret</code>, <code>Import</code>, or <code>Package</code> can be specified at a time.

<i>Type</i>: [Package Definition](ModuleFile-Parameters-Packages.md)
</dd>

<dt><code>Export</code></dt>
<dd>
The <code>Export</code> attribute specifies a path to the AWS Systems Manager Parameter Store. When the CloudFormation stack is deployed, the parameter value is published to the parameter store at the export path. If the export path starts with <code>/</code>, it will be used as an absolute path. Otherwise the export path is prefixed with <code>/{{Tier}}/{{Module}}/</code> to create an export path specific to the deployment tier.

The <code>Export</code> attribute cannot be used in conjunction with the <code>Secret</code> attribute.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Resource</code></dt>
<dd>
The parameter value corresponds to one or more AWS resources. A new, managed AWS resource is created when the <code>Resource</code> section is used without the <code>Value</code> attribute. Otherwise, one or more existing AWS resources are referenced. The resulting resource value (ARN, Queue URL, etc.) becomes the parameter value after initialization and can be retrieve during function initialization.

The <code>Resource</code> section cannot be used in conjunction with the <code>Secret</code> attribute.

<i>Required</i>: No

<i>Type</i>: [Resource Definition](ModuleFile-Parameters-Resources.md)
</dd>

<dt><code>Parameters</code></dt>
<dd>
The <code>Parameters</code> section contains nested parameter values and resources for the module.

<i>Required:</i> No

<i>Type:</i> List of Parameter Definition
</dd>
</dl>
