---
title: LambdaError Event - LambdaSharp Operational Events - LambdaSharp
description: Description of Lambda warnings/errors event emitted by LambdaSharp.Core
keywords: cloudwatch, events, modules, errors, warnings
---

# LambdaError Event

The _LambdaError_ event is emitted by _LambdaSharp.Core_ when ingesting a Lambda warning or error report from the associated the CloudWatch logs. Note that _Core Services_ must be enabled for these events to be emitted. The _LambdaError_ event captures information about the warning or error that occurred.

## Event Schema

```yaml
Type: String # LambdaError
Version: String # 2020-05-05
ModuleInfo: String
Module: String
ModuleId: String
FunctionId: String
FunctionName: String
Platform: String
Framework: String
Language: String
GitSha: String?
GitBranch: String?
RequestId: String?
Level: String
Fingerprint: String
Timestamp: Long
Message: String
Raw: String
Traces:
  - Exception:
      Type: String
      Message: String
      StackTrace: String?
    Frames: #List?
      - FileName: String?
        LineNumber: Int?
        ColumnNumber: Int?
        MethodName: String
```

## Event Properties


<dl>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> property holds the event type name. This property is always set to <code>"LambdaMetrics"</code>.

<i>Type</i>: String
</dd>

<dt><code>Version</code></dt>
<dd>

The <code>Version</code> property holds the event type version. This property is set to <code>"2020-05-05"</code>.

<i>Type</i>: String
</dd>

<dt><code>ModuleInfo</code></dt>
<dd>

The <code>ModuleInfo</code> property holds the LambdaSharp module name, version, and origin.

<i>Type</i>: String
</dd>

<dt><code>Module</code></dt>
<dd>

The <code>Module</code> property holds the LambdaSharp module name.

<i>Type</i>: String
</dd>

<dt><code>ModuleId</code></dt>
<dd>

The <code>ModuleId</code> property holds the stack name of the deployed LambdaSharp module.

<i>Type</i>: String
</dd>

<dt><code>FunctionId</code></dt>
<dd>

The <code>FunctionId</code> property holds the Lambda function name.

<i>Type</i>: String
</dd>

<dt><code>FunctionName</code></dt>
<dd>

The <code>FunctionName</code> property holds the Lambda function name.

<i>Type</i>: String
</dd>

<dt><code>AppId</code></dt>
<dd>

The <code>AppId</code> property holds the application identifier.

<i>Type</i>: String
</dd>

<dt><code>AppName</code></dt>
<dd>

The <code>AppName</code> property holds the application name.

<i>Type</i>: String
</dd>

<dt><code>AppDomainName</code></dt>
<dd>

The <code>AppDomainName</code> property holds the application domain name.

<i>Type</i>: String
</dd>

<dt><code>Platform</code></dt>
<dd>

The <code>Platform</code> property holds the Lambda or app execution platform.

<i>Type</i>: String
</dd>

<dt><code>Framework</code></dt>
<dd>

The <code>Framework</code> property holds the Lambda or app execution framework.

<i>Type</i>: String
</dd>

<dt><code>Language</code></dt>
<dd>

The <code>Language</code> property holds the Lambda or app implementation language.

<i>Type</i>: String
</dd>

<dt><code>GitSha</code></dt>
<dd>

The <code>GitSha</code> property holds the git SHA checksum when the module was deployed from a git repository, otherwise <code>null</code>.

<i>Type</i>: String or <code>null</code>
</dd>

<dt><code>GitBranch</code></dt>
<dd>

The <code>GitBranch</code> property holds the git branch name when the module was deployed from a git repository, otherwise <code>null</code>.

<i>Type</i>: String or <code>null</code>
</dd>

<dt><code>RequestId</code></dt>
<dd>

The <code>RequestId</code> property holds the Lambda function invocation request ID if available, otherwise <code>null</code>.

<i>Type</i>: String or <code>null</code>
</dd>

<dt><code>Level</code></dt>
<dd>

The <code>Level</code> property describes the severity level of the error log entry. One of <code>WARNING</code>, <code>ERROR</code>, or <code>FATAL</code>.

<i>Type</i>: String
</dd>

<dt><code>Fingerprint</code></dt>
<dd>

The <code>Fingerprint</code> property holds a hash value that can be used to group related <em>LambdaError</em> events.

<i>Type</i>: String
</dd>

<dt><code>Timestamp</code></dt>
<dd>

The <code>Timestamp</code> property holds the UNIX epoch in milliseconds when the error log entry was generated.

<i>Type</i>: String
</dd>

<dt><code>Message</code></dt>
<dd>

The <code>Message</code> property holds the message of the <em>LambdaError</em> event.

<i>Type</i>: String
</dd>

<dt><code>Raw</code></dt>
<dd>

The <code>Raw</code> property holds unprocessed error log entry.

<i>Type</i>: String
</dd>

<dt><code>Traces</code></dt>
<dd>

The <code>Traces</code> property describes the exception stack traces when available, otherwise <code>null</code>.

<i>Type</i>: List of exception stack traces or <code>null</code>


<dl>

<dt><code>Exception</code></dt>
<dd>

The <code>Exception</code> property holds information about the exception.

<i>Type</i>: String


<dl>

<dt><code>Type</code></dt>
<dd>

The <code>Type</code> property holds the full exception type name.

<i>Type</i>: String
</dd>

<dt><code>Message</code></dt>
<dd>

The <code>Message</code> property holds the exception message.

<i>Type</i>: String
</dd>

<dt><code>StackTrace</code></dt>
<dd>

The <code>StackTrace</code> property the unparsed exception stack trace when available, otherwise <code>null</code>.

<i>Type</i>: String or <code>null</code>
</dd>

</dl>


</dd>

<dt><code>Frames</code></dt>
<dd>

The <code>Frames</code> property describes the stack frames between where the exception was thrown and where it was caught when available, otherwise <code>null</code>.

<i>Type</i>: List of stack frames or <code>null</code>


<dl>

<dt><code>FileName</code></dt>
<dd>

The <code>FileName</code> property holds the source code file name of the stack trace when available, otherwise <code>null</code>.

<i>Type</i>: String or <code>null</code>
</dd>

<dt><code>LineNumber</code></dt>
<dd>

The <code>LineNumber</code> property holds the line number in the source code when available, otherwise <code>null</code>.

<i>Type</i>: Int or <code>null</code>
</dd>

<dt><code>ColumnNumber</code></dt>
<dd>

The <code>ColumnNumber</code> property holds the column number in the source code when available, otherwise <code>null</code>.

<i>Type</i>: Int or <code>null</code>
</dd>

<dt><code>MethodName</code></dt>
<dd>

The <code>MethodName</code> property holds the method name in which the stack frame is located.

<i>Type</i>: String
</dd>

</dl>


</dd>


</dl>


</dd>

</dl>

## Event Sample

```json
{
  "ModuleInfo": "LambdaSharp.BadModule:1.0-DEV@sandbox-lambdasharp-cor-deploymentbucketresource-9h53iqcat7uj",
  "Module": "LambdaSharp.BadModule",
  "ModuleId": "Sandbox-LambdaSharp-BadModule",
  "FunctionId": "Sandbox-LambdaSharp-BadModule-FailError-9ZWPJT2RI6V6",
  "FunctionName": "FailError",
  "Platform": "AWS Lambda (Unix 4.14.165.102)",
  "Framework": "dotnetcore3.1",
  "Language": "csharp",
  "GitSha": "DIRTY-a8f11ab357937e3cb87d512f08fc649c8261b1b8",
  "GitBranch": "WIP-v0.8",
  "RequestId": "f7d92855-628c-4285-ad83-fb7f10a81ec1",
  "Level": "ERROR",
  "Fingerprint": "F5F7309CD31CCF342EC3ED07679E79FD",
  "Timestamp": 1589411401688,
  "Message": "this exception was thrown on request",
  "Traces": [
    {
      "Exception": {
        "Type": "System.Exception",
        "Message": "this exception was thrown on request",
        "StackTrace": " at BadModule.FailError.Function.ProcessMessageAsync(FunctionRequest request) in C:\\LambdaSharp\\LambdaSharpTool\\Tests\\BadModule\\FailError\\Function.cs:line 36\n at LambdaSharp.ALambdaFunction`2.ProcessMessageStreamAsync(Stream stream)\n at LambdaSharp.ALambdaFunction.FunctionHandlerAsync(Stream stream, ILambdaContext context) in C:\\LambdaSharp\\LambdaSharpTool\\src\\LambdaSharp\\ALambdaFunction.cs:line 398"
      },
      "Frames": [
        {
          "FileName": "C:\\LambdaSharp\\LambdaSharpTool\\Tests\\BadModule\\FailError\\Function.cs",
          "LineNumber": 36,
          "MethodName": "MoveNext"
        },
        {
          "FileName": "System.Runtime.ExceptionServices.ExceptionDispatchInfo",
          "MethodName": "Throw"
        },
        {
          "FileName": "System.Runtime.CompilerServices.TaskAwaiter",
          "MethodName": "ThrowForNonSuccess(System.Threading.Tasks.Task task)"
        },
        {
          "FileName": "System.Runtime.CompilerServices.TaskAwaiter",
          "MethodName": "HandleNonSuccessAndDebuggerNotification(System.Threading.Tasks.Task task)"
        },
        {
          "FileName": "LambdaSharp.ALambdaFunction`2+<ProcessMessageStreamAsync>d__3[TRequest,TResponse]",
          "MethodName": "MoveNext"
        },
        {
          "FileName": "System.Runtime.ExceptionServices.ExceptionDispatchInfo",
          "MethodName": "Throw"
        },
        {
          "FileName": "System.Runtime.CompilerServices.TaskAwaiter",
          "MethodName": "ThrowForNonSuccess(System.Threading.Tasks.Task task)"
        },
        {
          "FileName": "System.Runtime.CompilerServices.TaskAwaiter",
          "MethodName": "HandleNonSuccessAndDebuggerNotification(System.Threading.Tasks.Task task)"
        },
        {
          "FileName": "C:\\LambdaSharp\\LambdaSharpTool\\src\\LambdaSharp\\ALambdaFunction.cs",
          "LineNumber": 398,
          "MethodName": "MoveNext"
        }
      ]
    }
  ],
  "Type": "LambdaError",
  "Version": "2020-05-05"
}
```
