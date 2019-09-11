# Nested Module

The `Module` definition specifies the creation of a nested module. Nested modules inherit the same λ# configuration as their parent module, except for the `Secrets` parameter, which must be passed on explicitly.

Nested module should **never** be updated directly. Instead, all updates must be triggered by their parent module. Updating a nested module directly can cause it to be in an unexpected state that will prevent the parent module from upating in the future.

## Syntax

```yaml
Nested: String
Description: String
Module: String
Parameters:
  ModuleParameters
DependsOn:
  - String
```

## Properties

<dl>

<dt><code>DependsOn</code></dt>
<dd>

The <code>DependsOn</code> attribute identifies items that must be created prior. For additional information, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-attribute-dependson.html">CloudFormation DependsOn Attribute</a>.

<i>Required</i>: No

<i>Type</i>: List of String
</dd>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the nested module description.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Module</code></dt>
<dd>

The <code>Module</code> attribute specifies the full name of the required module with an optional version and origin. The format must be <code>Namespace.Name[:Version][@Origin]</code>. Parts in brackets (<code>[ ]</code>) are optional. Without a version specifier, λ# uses the latest version it can find that is compatible with the λ# CLI. Without an origin, λ# uses the deployment bucket name of the active deployment tier as origin. Compilation fails if the λ# CLI cannot find a published module that matches the criteria.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Nested</code></dt>
<dd>

The <code>Nested</code> attribute specifies the item name. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Parameters</code></dt>
<dd>

The <code>Parameters</code> section specifies the parameters for the nested module. The λ# deployment parameters, such as <code>DeploymentBucketName</code> and <code>DeploymentPrefix</code>, are automatically provided and don't need to be specified.

<i>Required</i>: No

<i>Type</i>: Map
</dd>

</dl>


## Examples

### Creating a nested module and accessing it outputs

```yaml
- Nested: MyNestedModule
  Module: Acme.MyOtherModule:1.0
  Parameters:
    Message: !Sub "Hi from ${Module::Name}"

- Variable: NestedOutput
  Value: !GetAtt MyNestedModule.Outputs.OutputName
```
