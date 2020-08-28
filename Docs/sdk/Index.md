---
title: LambdaSharp SDK
description: LambdaSharp SDK Overview
keywords: sdk, overview, getting started, project, base class
---
![λ#](~/images/SDK.png)

# LambdaSharp SDK

The required LambdaSharp dependencies are automatically added when you use `lash new` to add a project to your module. In addition, `lash build` performs a compatibility check and issues a warning when a dependency is outdated.


## Creating a LambdaSharp Module

Create a folder for your first LambdaSharp module and open it your favorite terminal application.

Run the following command to create a `Module.yml` file in the current folder.
```bash
lash new module My.FirstModule
```

### Add a C# Lambda Function

Run the following command to add a generic C# Lambda function to your module.
```
lash new function --type generic MyFunction
```

### Add a Blazor WebAssembly App

Run the following command to add a Blazor WebAssembly app to your module.
```
lash new app MyApp
```

### Deploy Module

Run the following command to build and deploy your first module to your _Sandbox_ deployment tier.
```
lash deploy --tier Sandbox
```
