![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Functions Section

The `Functions` section, in the [Module](ModuleFile.md) file, may contain zero or more function definitions. Each definition corresponds to a .NET Core project that is compiled and deployed. The published Lambda functions are prefixed with `{{Tier}}-{{Module}}.` to uniquely distinguish them from other functions.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)

## Syntax

```yaml
Name: String
Description: String
Memory: Int
Timeout: Int
Project: String
Handler: String
Runtime: String
ReservedConcurrency: Int
Export: String
Environment:
  String: String
Sources:
  - SourceDefinition
```

## Properties

<dl>
<dt><code>Name</code></dt>
<dd>
The <code>Name</code> attribute specifies the function name used to publish the Lambda function.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute value is used by the AWS Lambda function.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Memory</code></dt>
<dd>
The <code>Memory</code> attribute specifies the memory limit for the lambda function. The value must be in the range of 128 MB up to 3008 MB, in 64 MB increments.
</pre>

<i>Required</i>: Yes

<i>Type</i>: Int
</dd>

<dt><code>Timeout</code></dt>
<dd>
The <code>Timeout</code> attribute specifies the execution time limit in seconds. The maximum value is 300 seconds.

<i>Required</i>: Yes

<i>Type</i>: Int
</dd>

<dt><code>Project</code></dt>
<dd>
The <code>Project</code> attribute specifies the relative path of the .NET Core project file location for the lambda function.

<i>Required</i>: Conditional. By default, the .NET Core project file is expected to be located in a sub-folder of the module file, following this naming convention: <code>{{Module}}.{{FunctionName}}/{{Module}}.{{FunctionName}}.csproj</code>. If that is not the case, then the <code>Project</code> attribute must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><code>Handler</code></dt>
<dd>
The <code>Handler</code> attribute specifies the fully qualified .NET Core method reference to the Lambda function handler.

<i>Required</i>: Conditional. By default, the .NET Core method reference is expected to be <code>{{Module}}.{{FunctionName}}::{{Namespace}}.Function::FunctionHandlerAsync</code> where <code>{{Namespace}}</code> is determined by inspecting the <code>&lt;RootNamespace&gt;</code> element of the .NET Core project file. If the Lambda function handler is not called <code>FunctionHandlerAsync</code>, or the class implemented it is not called <code>Function</code>, or the <code>&lt;RootNamespace&gt;</code> is not specified in the .NET Core project file, or the .NET Core assembly name is not <code>{{Module}}.{{FunctionName}}</code>, the the <code>Handler</code> attribute must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><code>Runtime</code></dt>
<dd>
The <code>Runtime</code> attribute specifies the Lambda runtime to use to run the function.

<i>Required</i>: Conditional. By default, the runtime is determined by inspecting the .NET Core project file. If the runtime cannot be determined automatically, then it must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><code>ReservedConcurrency</code></dt>
<dd>
The <code>ReservedConcurrency</code> attribute specifies the number of Lambda invocation slots to reserve for this Lambda function. The invocation slots are drawn from a global pool and once allocated by a function, cannot be used by any other Lambda function on the AWS account. At the same time, <code>ReservedConcurrency</code> value also specifies the maximum number of concurrent Lambda executions for the function. When omitted, the Lambda function has no limit in number of invocations and also does not prevent other Lambda functions from being invoked.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><code>Export</code></dt>
<dd>
The <code>Export</code> attribute specifies a path to the AWS Systems Manager Parameter Store. When the CloudFormation stack is deployed, the Lambda ARN is published to the parameter store at the export path. If the export path starts with <code>/</code>, it will be used as an absolute path. Otherwise the export path is prefixed with <code>/{{Tier}}/{{Module}}/</code> to create an export path specific to the deployment tier.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>Environment</code></dt>
<dd>
The <code>Environment</code> sections contains key-value pairs that correspond to custom <a href="https://docs.aws.amazon.com/lambda/latest/dg/env_variables.html">Lambda environment variables</a> which can be retrieved by the Lambda function during initialization. The attribute can be a plaintext value or a CloudFormation expression (e.g. <code>!Ref MyResource</code>).

<i>Required</i>: No

<i>Type</i>: Map of key-value pairs
</dd>

<dt><code>Sources</code></dt>
<dd>
The <code>Sources</code> section contains zero or more source definitions the Lambda function expects to be invoked by. Each source automatically grants the Lambda invocation permission to the invoking service/resource.

<i>Required</i>: No

<i>Type</i>: List of [Source Definition](ModuleFile-Functions-Sources.md)
</dd>
</dl>

