---
title: LambdaSharp CLI New Command - Create a Module
description: Create a new LambdaSharp module
keywords: cli, new, create, module
---
# Create New Module File

The `new module` command is used to create a new module definition.

## Arguments

The `new module` command takes a single argument that specifies the module name.

```bash
lash new module My.NewModule
```

## Options

<dl>

<dt><code>--working-directory &lt;PATH&gt;</code></dt>
<dd>

(optional) New module directory (default: current directory)
</dd>

</dl>

## Examples

### Create a new module in the current folder

__Using PowerShell/Bash:__
```bash
lash new module My.NewModule
```

Output:
```
LambdaSharp CLI (v0.5) - Create new LambdaSharp module, function, or resource
Created module definition: Module.yml

Done (finished: 1/18/2019 1:13:58 PM; duration: 00:00:00.0143553)
```
