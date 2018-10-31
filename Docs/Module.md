![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Definition

A λ# module definition specifies the input parameters, output values, resources, and functions of a module.

Input parameters can either be provided at module deployment time or imported from other modules using cross-module references. In addition, optional input parameters can also act as conditional resources that are created when an input parameter is omitted.

Output values can be accessed by other modules using cross-module references. Output values can also be definitions for custom resource handlers that can be used by other modules during their deployment.

Resources are defined in the `Variables` section and can be configured using input parameters or other module variables.

Functions can be wired up to respond to various event sources, such as SQS, SNS, API Gateway, or even Slack Commands. Functions can be implemented using .NET Core projects or Javascript files.

The λ# CLI compiles the module definition into a CloudFormation template, uploads all assets, and then creates/updates the CloudFormation stack.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)

## Syntax

```yaml
Module: String
Version: String
Description: String
Secrets:
  - String
Inputs:
  - InputDefinition
Outputs:
  - OutputDefinition
Variables:
  - VariableDefinition
Functions:
  - FunctionDefinition
```

## Properties

> TODO: inputs, outputs

<dl>

<dt><code>Module</code></dt>
<dd>
The <code>Module</code> attribute specifies the name of the λ# module. It is used as the default name for module deployments. The module name can be accessed as a variable in <code>!Sub</code> operations using <code>${Module::Name}</code>.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>Version</code></dt>
<dd>
The <code>Version</code> attribute specifies the version of the λ# module. The format of the version must be <code>Major.Minor[.Build[.Revision]][-Suffix]</code>. Components in square brackets (<code>[]</code>) are optional and can be omitted. The module version can be accessed as a variable in <code>!Sub</code> operations using the <code>${Module::Version}}</code>.

<i>Required:</i> No

<i>Type:</i> String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the description for the CloudFormation stack.

<i>Required:</i> No

<i>Type:</i> String
</dd>

<dt><code>Secrets</code></dt>
<dd>
The <code>Secrets</code> section specifies which KMS keys can be used to decrypt parameter values. The module IAM role will get permission to use these keys (i.e. `mks:Decrypt`).

<i>Required:</i> No

<i>Type:</i> List of String (see [Secrets Section](Module-Secrets.md))
</dd>

<dt><code>Inputs</code></dt>
<dd>
The <code>Inputs</code> section specifies the input parameter definitions for the module.

<i>Required:</i> No

<i>Type:</i> List of [Input Parameter Definition](Module-Inputs.md)
</dd>

<dt><code>Outputs</code></dt>
<dd>
The <code>Output</code> section specifies the output value definitions for the module.

<i>Required:</i> No

<i>Type:</i> List of [Output Value Definition](Module-Outputs.md)
</dd>

<dt><code>Variables</code></dt>
<dd>
The <code>Variables</code> section defines the literal values and resources for the module.

<i>Required:</i> No

<i>Type:</i> List of [Variable Definition](Module-Variables.md)
</dd>

<dt><code>Functions</code></dt>
<dd>
The <code>Functions</code> section defines the lambda functions that are part of the module.

<i>Required:</i> No

<i>Type:</i> List of [Function Definition](Module-Function.md)
</dd>

</dl>
