---
title: LambdaSharp CLI New Command - Add Blazor App to Module
description: Add a Blazor App to a LambdaSharp module
keywords: cli, new, create, app, blazor, module
---
# Add New App to Module File

The `new app` command is used to add a [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) app to an existing module.

## Arguments

The `new app` command takes a single argument that specifies the app name.

```bash
lash new app MyApp
```

## Options

<dl>

<dt><code>--namespace &lt;VALUE&gt;</code></dt>
<dd>

(optional) Root namespace for project (default: same as app name)
</dd>

<dt><code>--working-directory &lt;VALUE&gt;</code></dt>
<dd>

(optional) New app project parent directory (default: current directory)
</dd>

</dl>

## Examples

### Create a new Blazor WebAssembly app

__Using PowerShell/Bash:__
```bash
lash new app MyApp
```

Output:
```
LambdaSharp CLI (v0.8.1.0) - Create new LambdaSharp app
Created file: MyApp\App.razor
Created file: MyApp\_Imports.razor
Created file: MyApp\MyApp.csproj
Created file: MyApp\Pages\Index.razor
Created file: MyApp\Pages\TodoApp.razor
Created file: MyApp\Program.cs
Created file: MyApp\Shared\MainLayout.razor
Created file: MyApp\Shared\Todo.cs
Created file: MyApp\Shared\TodoListItem.razor
Created file: MyApp\wwwroot\favicon.ico
Created file: MyApp\wwwroot\index.html

Done (finished: 8/13/2020 4:46:35 PM; duration: 00:00:00.1552764)
```
