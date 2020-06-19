---
title: Function Declaration - Module
description: LambdaSharp YAML syntax for Lambda functions
keywords: lambda, function, declaration, syntax, yaml, cloudformation
---
# Function

The `Function` definition specifies a Lambda function for deployment. Each definition is compiled and uploaded as part of the deployment process. The deployed Lambda function is prefixed with `${DeploymentPrefix}` to uniquely distinguish is from other functions.

## Syntax

```yaml
Function: String
Description: String
Scope: ScopeDefinition
If: String or Expression
Memory: Int
Timeout: Int
Project: String
Handler: String
Runtime: String
Language: String
Pragmas:
  - PragmaDefinition
Environment:
  String: String
Properties:
  ResourceProperties
Sources:
  - SourceDefinition
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the description of the Lambda function.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Environment</code></dt>
<dd>

The <code>Environment</code> sections specifies key-value pairs that correspond to custom <a href="https://docs.aws.amazon.com/lambda/latest/dg/env_variables.html">Lambda environment variables</a> which can be retrieved by the Lambda function during initialization. The attribute can be a plaintext value or a CloudFormation expression (e.g. <code>!Ref MyResource</code>).

<i>Required</i>: No

<i>Type</i>: Map of key-value pair Expressions
</dd>

<dt><code>Function</code></dt>
<dd>

The <code>Function</code> attribute specifies the item name for the Lambda function.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Handler</code></dt>
<dd>

The <code>Handler</code> attribute specifies the fully qualified method reference to the Lambda function handler.

<i>Required</i>: Conditional. By default, the .NET Core method reference is expected to be <code>${Module::Name}.${FunctionName}::${Namespace}.Function::FunctionHandlerAsync</code> where <code>${Namespace}</code> is determined by inspecting the <code>&lt;RootNamespace&gt;</code> element of the .NET Core project file. If the Lambda function handler is not called <code>FunctionHandlerAsync</code>, or the class implemented it is not called <code>Function</code>, or the <code>&lt;RootNamespace&gt;</code> is not specified in the .NET Core project file, the the <code>Handler</code> attribute must be specified. Otherwise, it can be omitted. For javascript functions, the <code>Handler</code> is set to <code>index.handler</code> by default.

<i>Type</i>: String
</dd>

<dt><code>If</code></dt>
<dd>

The <code>If</code> attribute specifies a condition that must be met for the Lambda function to be included in the deployment. The condition can either the name of a <code>Condition</code> item or a logical expression.

<i>Required</i>: No.

<i>Type</i>: String or Expression
</dd>

<dt><code>Memory</code></dt>
<dd>

The <code>Memory</code> attribute specifies the memory limit for the lambda function. The value must be in the range of 128 MB up to 3008 MB, in 64 MB increments.
</pre>

<i>Required</i>: Yes

<i>Type</i>: Int
</dd>

<dt><code>Pragmas</code></dt>
<dd>

The <code>Pragmas</code> section specifies directives that change the default compiler behavior.

<i>Required:</i> No

<i>Type:</i> List of [Pragma Definition](Module-Pragmas.md)
</dd>

<dt><code>Project</code></dt>
<dd>

The <code>Project</code> attribute specifies the relative path of the function project file or its folder.

<i>Required</i>: Conditional. By default, the .NET Core project file is expected to be located in a sub-folder of the module definition. The name of the sub-folder and project file are expected to match the function name. If that is not the case, then the <code>Project</code> attribute must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><code>Properties</code></dt>
<dd>

The <code>Properties</code> section specifies additional options that can be specified for a Lambda function (see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-resource-lambda-function.html"><code>AWS::Lambda::Function</code></a> CloudFormation type). This section is copied verbatim into the CloudFormation template and can use <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/intrinsic-function-reference.html">CloudFormation intrinsic functions</a> (e.g. <code>!Ref</code>, <code>!Join</code>, <code>!Sub</code>, etc.) for referencing other resources.

<i>Required</i>: No

<i>Type</i>: Map
</dd>

<dt><code>Runtime</code></dt>
<dd>

The <code>Runtime</code> attribute specifies the Lambda runtime to use to run the function.

<i>Required</i>: Conditional. By default, the runtime is determined by inspecting the function sub-folder. If the runtime cannot be determined automatically, then it must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><code>Scope</code></dt>
<dd>

The <code>Scope</code> attribute specifies which functions need to have access to this item. The <code>Scope</code> attribute can be a comma-separated list or a YAML list of function names. If all function need the item, then <code>all</code> can be used as a wildcard. In addition, the <code>public</code> can be used to export the item from the module.

<i>Required</i>: No

<i>Type</i>: Comma-delimited String or List of String
</dd>

<dt><code>Sources</code></dt>
<dd>

The <code>Sources</code> section specifies zero or more source definitions the Lambda function expects to be invoked by. Each source automatically grants the Lambda invocation permission to the invoking service.

<i>Required</i>: No

<i>Type</i>: List of [Source Definition](Module-Function-Sources.md)
</dd>

<dt><code>Timeout</code></dt>
<dd>

The <code>Timeout</code> attribute specifies the execution time limit in seconds. The maximum value is 900 seconds (15 minutes).

<i>Required</i>: Yes

<i>Type</i>: Int
</dd>

</dl>

## Examples

### A vanilla Lambda function

```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 15
```

### A Lambda function with an SNS event source

```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 15
  Sources:
    - Topic: MySnsTopic
```

### A conditional Lambda function

```yaml
- Condition: IsFunctionWanted
  Value: !Equals [ !Ref WantFunctionParameter, "yes" ]

- Function: MyFunction
  If: IsFunctionWanted
  Memory: 128
  Timeout: 15
```

The above definitions can be expressed more concisely if the `Condition` item never used by anywhere else.

```yaml
- Function: MyFunction
  If: !Equals [ !Ref WantFunctionParameter, "yes" ]
  Memory: 128
  Timeout: 15
```

### A Lambda function with properties

```yaml
- Function: MyFunction
  Memory: 128
  Timeout: 15
  Properties:
    ReservedConcurrentExecutions: 1
    VpcConfig:
      SecurityGroupIds: !Split [ ",", !Ref SecurityGroupIds ]
      SubnetIds: !Split [ ",", !Ref SubnetIds ]
```
