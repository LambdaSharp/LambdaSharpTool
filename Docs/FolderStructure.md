![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Folder Structure

λ# modules must follow a consistent folder organization. The root folder must contain the `Module.yml` file. Each function listed in the `Module.yml` must have a corresponding folder the naming convention `{AppName}.{FunctionName}`. The .NET Core projects file (`.csproj`) for each function should be contained within said sub-folder and be named with the same naming convention. (e.g. `{AppName}.{FunctionName}.csproj`).

* `GettingStarted`
  * `Module.yml`
  * `GettingStarted.SlackCommand`
    * `GettingStarted.SlackCommand.csproj`

Furthermore, the project file should contain the `<RootNamespace>` element to define the root namespace of the project. When missing, the function configuration will need to explicitly list the name of the function handler using the `Handler` setting.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <Deterministic>true</Deterministic>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
    <RootNamespace>GettingStarted.SlackCommand</RootNamespace>
  </PropertyGroup>
  ...
  <ItemGroup>
    <DotNetCliToolReference Include="Amazon.Lambda.Tools" Version="2.1.3"/>
  </ItemGroup>
</Project>
```

Furthermore, the function handler should be in a class called `Function` and the method should be called `FunctionHandler`. When different, the function configuration will need to explicitly list the name of the function handler using the `Handler` setting.

In order for the λ# tool to work properly the .NET Core project file must contain a reference to `Amazon.Lambda.Tools`.

```csharp
namespace GettingStarted.SlackCommand {

    public class Function : ALambdaSlackCommandFunction {

        // ...implementation code...
    }
}
```
