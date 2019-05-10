# Function Pragmas

Pragmas are used to change the default processing behavior of the λ# CLI. They are generally only required for very specific situations.

|Pragma                                 |Definition                           |
|---------------------------------------|-------------------------------------|
|`no-assembly-validation`               |Don't validate that the λ# assemblies referenced by the .csproj file are consistent with the CLI version (only works for C# projects)|
|`no-dead-letter-queue`                 |Don't add the λ# Dead-Letter Queue to the function|
|`no-function-registration`             |Don't create a function registration|
|`no-handler-validation`                |Don't validate if the Lambda function handler can be found in the compiled assembly|
|`no-wildcard-scoped-variables`         |Don't include function in wildcard (`*`) scopes|
