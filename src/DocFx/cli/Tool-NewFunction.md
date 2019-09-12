---
title: LambdaSharp CLI New Command - Add Lambda Function to Module
description: Add a Lambda function to a LambdaSharp module
keywords: cli, new, create, lambda, module
---
# Add New Function to Module File

The `new function` command is used to add a function to an existing module. The command can either create a C# or a Javascript function using the `--language` option.

## Arguments

The `new function` command takes a single argument that specifies the function name.

```bash
lash new function MyNewFunction
```

## Options

<dl>

<dt><code>--namespace &lt;VALUE&gt;</code></dt>
<dd>

(optional) Root namespace for project (default: same as function name)
</dd>

<dt><code>--working-directory &lt;VALUE&gt;</code></dt>
<dd>

(optional) New function project parent directory (default: current directory)
</dd>

<dt><code>--framework|-f &lt;VALUE&gt;</code></dt>
<dd>

(optional) Target .NET framework (default: 'netcoreapp2.1')
</dd>

<dt><code>--language|-l &lt;LANGUAGE&gt;</code></dt>
<dd>

(optional) Select programming language for generated code (default: csharp)
</dd>

<dt><code>--type|-t &lt;TYPE&gt;</code></dt>
<dd>

(optional) Function type (one of: apigateway, customresource, generic, queue, schedule, topic, websocket; default: prompt)
</dd>

<dt><code>--timeout &lt;SECONDS&gt;</code></dt>
<dd>

(optional) Function timeout in seconds (default: 30)
</dd>

<dt><code>--memory &lt;MB&gt;</code></dt>
<dd>

(optional) Function memory in megabytes (default: 256)
</dd>

</dl>

## Examples

### Create a new, generic Lambda C# function

__Using PowerShell/Bash:__
```bash
lash new function --type generic MyNewFunction
```

Output:
```
LambdaSharp CLI (v0.5) - Create new LambdaSharp module, function, or resource
Created project file: MyNewFunction\MyNewFunction.csproj
Created function file: MyNewFunction\Function.cs

Done (finished: 1/18/2019 1:17:14 PM; duration: 00:00:00.1047835)
```

### Create a new Javascript function

__Using PowerShell/Bash:__
```bash
lash new function --language javascript MyNewFunction
```

Output:
```
LambdaSharp CLI (v0.5) - Create new LambdaSharp module or function
Created function file: MyNewFunction\index.js

Done (finished: 1/18/2019 1:17:47 PM; duration: 00:00:00.1073753
```
