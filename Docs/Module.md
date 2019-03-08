![λ#](LambdaSharpLogo.png)

# LambdaSharp Module

A λ# module is divided into three main components: details about the module, required dependencies, and item definitions, such as parameters, variables, resources, and functions.

Parameter values are provided at module deployment time. Optionally, parameters can act as conditional resources that are created when a parameter value is omitted.

Variables hold intermediate results that can be shared with other item definitions in the module. Variables are inlined during compilation and don't appear in the final output unless shared publicly.

Parameters, variables, and resources can be shared with other modules by making them `public`. These can then be imported using cross-module references.

Functions can be wired up to respond to various event sources, such as SQS, SNS, API Gateway, or even Slack Commands. Functions can be implemented using C# or Javascript.

The λ# CLI `build` command compiles the module into a CloudFormation template. The `publish` command uploads the assets to the deployment bucket. Finally, the `deploy` command creates/updates a CloudFormation stack.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Module: String
Version: String
Description: String
Pragmas:
  - PragmaDefinition
Secrets:
  - String
Using:
  - UsingDefinition
Items:
  - ItemDefinition
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the description for the CloudFormation stack.

<i>Required:</i> No

<i>Type:</i> String
</dd>

<dt><code>Items</code></dt>
<dd>
The <code>Items</code> section specifies the items defined in the module, such as parameters, variables, resources, conditions, mappings, functions, nested modules, resource type definitions, macro definitions, and module imports.

<i>Required:</i> No

<i>Type:</i> List of [Item Definition](Module-Items.md)
</dd>

<dt><code>Module</code></dt>
<dd>
The <code>Module</code> attribute specifies the owner and module names. The <code>Module</code> attribute must be formatted as <code>Owner.Name</code>.
The module owner and name can be retrieved using the <code>!Ref</code> operations with <code>Module::Owner</code> and <code>Module::Name</code>, respectively. The full name can be retrieved using <code>Module::FullName</code>.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><code>Pragmas</code></dt>
<dd>
The <code>Pragmas</code> section specifies directives that change the default compiler behavior.

<i>Required:</i> No

<i>Type:</i> List of [Pragma Definition](Module-Pragmas.md)
</dd>

<dt><code>Using</code></dt>
<dd>
The <code>Using</code> section specifies λ# modules that are used by this module. During the build phase, the manifests of the used modules are imported to validate their parameters and attributes. During the deploy phase, the used modules are automatically deployed when missing.

<i>Required:</i> No

<i>Type:</i> List of [Using Definition](Module-Requires.md)
</dd>

<dt><code>Secrets</code></dt>
<dd>
The <code>Secrets</code> section specifies which KMS keys can be used to decrypt parameter values. The module IAM role will get permission to use these keys (<code>mks:Decrypt</code> etc.).

<i>Required:</i> No

<i>Type:</i> List of String (see [Secrets Section](Module-Secrets.md))
</dd>

<dt><code>Version</code></dt>
<dd>
The <code>Version</code> attribute specifies the version of the λ# module. The format of the version must be <code>Major.Minor[.Build[.Revision]][-Suffix]</code>. Components in square brackets (<code>[]</code>) are optional and can be omitted. The presence of the <code>-Suffix</code> element indicates a pre-release version.

The module version can be accessed as a variable in <code>!Sub</code> operations using the <code>${Module::Version}</code>.

<i>Required:</i> No

<i>Type:</i> String
</dd>

</dl>
