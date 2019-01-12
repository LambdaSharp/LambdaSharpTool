![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Function Definition

The `Function` definition specifies a Lambda function for deployment. Each definition is compiled and uploaded as part of the deployment process. The deployed Lambda function is prefixed with `${Module::Id}-` to uniquely distinguish is from other functions.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Function: String
Description: String
Memory: Int
Timeout: Int
Project: String
Handler: String
Runtime: String
Language: String
ReservedConcurrency: Int
VPC:
  VpcDefinition
Environment:
  String: String
Sources:
  - SourceDefinition
```

## Properties

<dl>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the description of the AWS Lambda function.

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
The <code>Function</code> attribute specifies the function name used to deploy the Lambda function.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Handler</code></dt>
<dd>
The <code>Handler</code> attribute specifies the fully qualified .NET Core method reference to the Lambda function handler.

<i>Required</i>: Conditional. By default, the .NET Core method reference is expected to be <code>${Module::Name}.${FunctionName}::${Namespace}.Function::FunctionHandlerAsync</code> where <code>${Namespace}</code> is determined by inspecting the <code>&lt;RootNamespace&gt;</code> element of the .NET Core project file. If the Lambda function handler is not called <code>FunctionHandlerAsync</code>, or the class implemented it is not called <code>Function</code>, or the <code>&lt;RootNamespace&gt;</code> is not specified in the .NET Core project file, the the <code>Handler</code> attribute must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><code>Memory</code></dt>
<dd>
The <code>Memory</code> attribute specifies the memory limit for the lambda function. The value must be in the range of 128 MB up to 3008 MB, in 64 MB increments.
</pre>

<i>Required</i>: Yes

<i>Type</i>: Int
</dd>

<dt><code>Project</code></dt>
<dd>
The <code>Project</code> attribute specifies the relative path of the .NET Core project file location for the lambda function.

<i>Required</i>: Conditional. By default, the .NET Core project file is expected to be located in a sub-folder of the module definition. The name of the sub-folder and project file are expected to match the function name. If that is not the case, then the <code>Project</code> attribute must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><code>ReservedConcurrency</code></dt>
<dd>
The <code>ReservedConcurrency</code> attribute specifies the number of Lambda invocation slots to reserve for this Lambda function. The invocation slots are drawn from a global pool and once allocated by a function, cannot be used by any other Lambda function on the AWS account. At the same time, <code>ReservedConcurrency</code> value also specifies the maximum number of concurrent Lambda executions for the function. When omitted, the Lambda function has no limit in number of invocations and also does not prevent other Lambda functions from being invoked.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><code>Runtime</code></dt>
<dd>
The <code>Runtime</code> attribute specifies the Lambda runtime to use to run the function.

<i>Required</i>: Conditional. By default, the runtime is determined by inspecting the function sub-folder. If the runtime cannot be determined automatically, then it must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><code>Timeout</code></dt>
<dd>
The <code>Timeout</code> attribute specifies the execution time limit in seconds. The maximum value is 900 seconds (15 minutes).

<i>Required</i>: Yes

<i>Type</i>: Int
</dd>

<dt><code>Sources</code></dt>
<dd>
The <code>Sources</code> section specifies zero or more source definitions the Lambda function expects to be invoked by. Each source automatically grants the Lambda invocation permission to the invoking service.

<i>Required</i>: No

<i>Type</i>: List of [Source Definition](Module-Function-Sources.md)
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
