---
title: Pragmas Section - App
description: LambdaSharp app configuration pragmas
keywords: app, pragma, section, configuration, syntax, yaml, cloudformation
---
# App Pragmas

Pragmas are used to change the default processing behavior of the LambdaSharp CLI. They are generally only required for very specific situations.

|Pragma                                 |Definition                           |
|---------------------------------------|-------------------------------------|
|`no-assembly-validation`               |Don't validate that the LambdaSharp assemblies referenced by the .csproj file are consistent with the CLI version (only works for C# projects)
|`no-registration`                      |Don't create an app registration

## Examples

### A Lambda function with pragmas

```yaml
- App: MyApp
  Pragmas:
    - no-registration
```
