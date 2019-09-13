---
title: LambdaSharp SDK
description: LambdaSharp SDK Overview
keywords: sdk, overview, getting started, project, base class
---
![λ#](~/images/SDK.png)

# LambdaSharp SDK

The LambdaSharp assembly is automatically added when you use `lash` to add a function to your module. In addition, `lash build` shows a warning when the referenced LambdaSharp assembly is outdated.

## Creating a LambdaSharp Project

Create a folder for your first LambdaSharp module and open it your favorite terminal application.

Run the following command to create a `Module.yml` file in the current folder.
```bash
lash new module My.FirstModule
```

Run the following command to create a new C# function project add it to your module.
```
lash new function MyFunction
```

Run the following command to build and deploy your first module to your _Sandbox_ deployment tier.
```
lash deploy --tier Sandbox
```

## Adding the LambdaSharp Assembly

The LambdaSharp assembly provides base classes and utility methods for Lambda functions.

The assembly is referenced automatically when adding new functions to a module using `lash new function`. A a reference can be added manually, by using the `dotnet` CLI.

```bash
dotnet add package LambdaSharp
```