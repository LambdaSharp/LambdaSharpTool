---
title: LambdaSharp Glossary
description: Glossary of LambdaSharp terminology
keywords: glossary, definition, terminology, terms, keywords
---

# Glossary

<dl>

<dt><b>Artifact</b></dt>
<dd>

A file that is part of the published module.
</dd>

<dt><b>Asset</b></dt>
<dd>

Deprecated, use <i>Artifact</i> instead. A file that is part of the published module.
</dd>

<dt><b>Attribute</b></dt>
<dd>

A YAML mapping for a single value or list of values.
</dd>

<dt><b>Build Process</b></dt>
<dd>

The process by which the LambdaSharp CLI converts the source YAML module file into a CloudFormation JSON template file. The contents of the source are analyzed during the build process to detect errors, such as missing properties required to initialize a resource or references to undefined variables.
</dd>

<dt><b>Core</b></dt>
<dd>

The foundational CloudFormation template required to deploy and run modules.
</dd>

<dt><b>Cross-Module Reference</b></dt>
<dd>

A value imported from another module using <code>!ImportValue</code> where the source module is configurable. Optionally, the source module can also be replaced with a fixed value.
</dd>

<dt><b>Deployment Process</b></dt>
<dd>

The process by which the LambdaSharp CLI creates a CloudFormation stack from a CloudFormation template that was created from a LambdaSharp module. The deployment process checks for dependencies and installs them if needed. During the deployment process, the LambdaSharp CLI uses interactive prompts for obtain values for missing parameters. In addition, the LambdaSharp CLI supplies required parameters, such as the deployment bucket name and deployment tier prefix to launch the CloudFormation stack.
</dd>

<dt><b>Deployment Tier</b></dt>
<dd>

A deployment tier is used to isolate deployments from each other on a single AWS account. Deployment tiers are use commonly used to segment deployment between <i>production</i>, <i>staging</i>, and <i>test</i>.
</dd>

<dt><b>Import</b></dt>
<dd>

(see <i>Cross-Module Reference</i>)
</dd>

<dt><b>Module Definition</b></dt>
<dd>

A module is a CloudFormation template that follows the LambdaSharp conventions. LambdaSharp is a compiler that translates the higher-level LambdaSharp constructs into plain CloudFormation declarations. Only templates that follow the compiler conventions are called modules.
</dd>

<dt><b>Resource Type</b></dt>
<dd>

A resource type is the definition for a custom resource. Similar to built-in AWS CloudFormation types, it provides the list of <i>properties</i> that can be set and <i>attributes</i> that can be retrieved.
</dd>

<dt><b>Package</b></dt>
<dd>

A package is a compressed zip archive of files.
</dd>

<dt><b>Parameter</b></dt>
<dd>

A parameter is a variable that is set at module deployment time. A parameter can be optional by providing a default value. A parameter can constrained by a list of values or a regular expression. See documentation about <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/parameters-section-structure.html">CloudFormation parameters</a> for more information.
</dd>

<dt><b>Publishing Process</b></dt>
<dd>

The process by which a built module is made available for deployment.
</dd>

<dt><b>Section</b></dt>
<dd>

A YAML mapping for another YAML mapping.
</dd>

<dt><b>Tier</b></dt>
<dd>

(see <i>Deployment Tier</i>)
</dd>

</dl>
