---
title: LambdaSharp CLI New Command - Add Declaration to Module
description: Add a declaration to a LambdaSharp module
keywords: cli, new, create, cloudformation, resource, app, blazor, lambda, module
---
# Create New Module -or- Add Declaration to Module File

The `new` command creates a new module file if none exists. If one exists, the command prompts to add an AWS resource, Blazor WebAssembly app, or Lambda function declaration to the module file.

To add a specific declaration directly, use one of the following commands:
* [`lash new resource`](~/cli/Tool-New-Resource.md)
* [`lash new app`](~/cli/Tool-New-App.md)
* [`lash new function`](~/cli/Tool-New-Function.md)

## Options

<dl>

<dt><code>--namespace &lt;VALUE&gt;</code></dt>
<dd>

(optional) Root namespace for project (default: same as app/function name)
</dd>

</dl>

## Examples

### Create a new module in an empty folder

__Using PowerShell/Bash:__
```bash
lash new
```

Output:
```
LambdaSharp CLI (v0.8.1.5) - Create new LambdaSharp module, function, or resource
|=> Enter the module name: My.Module
Created module definition: Module.yml

Done (finished: 12/16/2020 9:38:43 PM; duration: 00:00:03.8781610)
```

### Add a declaration to a module

__Using PowerShell/Bash:__
```bash
lash new
```

Output:
```
LambdaSharp CLI (v0.8.1.5) - Create new LambdaSharp module, function, or resource
Select declaration to add:
1. AWS Resource
2. Blazor WebAssembly App
3. Lambda Function
|=> Enter a choice: 3

|=> Enter the function name: MyFunction

Select function type:
1. ApiGateway
2. ApiGatewayProxy
3. CustomResource
4. Finalizer
5. Generic
6. Queue
7. Schedule
8. Topic
9. WebSocket
10. WebSocketProxy
|=> Enter a choice: 5
Created project file: MyFunction\MyFunction.csproj
Created function file: MyFunction\Function.cs

Done (finished: 12/16/2020 9:40:48 PM; duration: 00:00:09.3108812)
```
