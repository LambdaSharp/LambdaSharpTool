![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Pragmas

Pragmas are used to change the default processing behavior of the λ# CLI. They are generally only required for very specific situations.

## Function Pragmas

|Pragma                                 |Definition                           |
|---------------------------------------|-------------------------------------|
|`no-assembly-validation`               |Don't validate that the λ# assemblies referenced by the .csproj file are consistent with the CLI version (only works for C# projects)|
|`no-dead-letter-queue`                 |Don't add the λ# Dead-Letter Queue to the function|
|`no-function-registration`             |Don't create a function registration|
|`no-handler-validation`                |Don't validate if the Lambda function handler can be found in the compiled assembly|
|`no-wildcard-scoped-variables`         |Don't include function in wildcard (`*`) scopes|


## Module Pragmas

|Pragma                                 |Definition                           |
|---------------------------------------|-------------------------------------|
|`no-core-version-check`                |Don't check if the λ# Core and CLI versions match|
|`no-lambdasharp-dependencies`          |Don't reference λ# Core resources (DLQ, Logging Stream, etc.)|
|`no-module-registration`               |Don't create a module registration|
|`sam-transform`                        |Add SAM template transform to CloudFormation output|

## Resource Pragmas

|Pragma                                 |Definition                           |
|---------------------------------------|-------------------------------------|
|`no-type-validation`                   |Don't validate attributes on resource|
