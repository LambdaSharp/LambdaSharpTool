![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module

The λ# module file defines the parameters and functions of a module. Parameters can be either values or AWS resources. Functions are .NET Core projects that are wired up to various invocation sources defined in the module file. Parameters, resources, and their access permissions are shared across all functions that are part of the same module. The λ# tool generates the CloudFormation template, compiles .NET Core projects, uploads all assets, and automatically creates/updates the CloudFormation stack.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)

## Syntax

```yaml
Name: String
Version: String
Description: String
Variables:
  VariablesDefinition
Secrets:
  - String
Parameters:
  - ParameterDefinition
Functions:
  - FunctionDefinition
```

## Properties

<dl>
<dt><code>Name</code></dt>
<dd>
The <code>Name</code> attribute defines the name of the λ# module. It is used as prefix for CloudFormation resources, as well as to access to the parameter store. The module name can be accessed as a variable in string substitutions using the <code>{{Module}}</code> notation.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>Version</code></dt>
<dd>
The <code>Version</code> attribute defines the version of the λ# module. It is exported to the parameter store to track the version of deployed modules. The module version can be accessed as a variable in string substitutions using the <code>{{Version}}</code> notation.

The format of the version must be <code>Major.Minor[.Build[.Revision]]</code>. Components in square brackets (<code>[]</code>) are optional and can be omitted.

<b>NOTE</b>: The `Version` attribute has not impact on whether a CloudFormation stack is updated. That determination depends  entirely on the parameters and functions of the λ# module.

<i>Required:</i> No

<i>Type:</i> String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute value is shown with the CloudFormation stack.

<i>Required:</i> No

<i>Type:</i> String
</dd>

<dt><code>Variables</code></dt>
<dd>
The <code>Variables</code> sections is an optional dictionary of key-value pairs. Variables are used in string substitutions to make it easy to change settings in the module file.

<i>Required:</i> No

<i>Type:</i> [Variables Definition](ModuleFile-Variables.md)
</dd>

<dt><code>Secrets</code></dt>
<dd>
The <code>Secrets</code> section lists which KMS keys can be used to decrypt parameter values. The module IAM role will get permission to use these keys (i.e. `mks:Decrypt`).

<i>Required:</i> No

<i>Type:</i> List of String (see [Secrets Section](ModuleFile-Secrets.md))
</dd>

<dt><code>Parameters</code></dt>
<dd>
The <code>Parameters</code> section contains the parameter values and resources for the module. In addition, these values can be published to the AWS Systems Manager Parameter Store for easy access by other modules and sysadmins.

<i>Required:</i> No

<i>Type:</i> List of [Parameter Definition](ModuleFile-Parameters.md)
</dd>

<dt><code>Functions</code></dt>
<dd>
The <code>Functions</code> section contains the lambda functions that are part of the module. All functions receive the same IAM role and have equal access to all parameters.

<i>Required:</i> No

<i>Type:</i> List of [Function Definition](ModuleFile-Functions.md)
</dd>
</dl>
