![λ#](../../Docs/images/LambdaSharpLogo.png)

# LambdaSharp Blazor WebAssembly with CloudWatch Events Sample

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

> **NOTE:** The [.NET Core SDK version 3.1.201 or later](https://dotnet.microsoft.com/download/dotnet-core/3.1) is required to use Blazor WebAssembly template. Confirm the installed .NET Core SDK version by running `dotnet --version` in a command shell.

Make sure to check out the [Get started with Blazor WebAssembly](https://docs.microsoft.com/en-us/aspnet/core/blazor/get-started?view=aspnetcore-3.1&tabs=visual-studio-code) page to get started.


## Module Definition

The following module definition does the following:
1. It compiles the _MyBlazorApp_ Blazor WebAssembly app project.
1. It creates a new S3 bucket for deploying the Blazor WebAssembly assets.
1. It create a REST API for the Blazor WebAssembly to integrate with CloudWatch Logs, CloudWatch Metrics, and CloudWatch EventBridge.
1. It shows the Blazor WebAssembly application website URL after the stack has been created.

```yaml
Module: Sample.BlazorEventsSample
Description: A sample module showing how to deploy a Blazor WebAssembly website
Items:

  - App: MyBlazorApp
    Description: Sample Blazor WebAssembly application
    Sources:
      - EventBus: default
        Pattern:
          Source:
            - !Ref MyBlazorApp::EventSource
          DetailType:
            - Sample.BlazorEventsSample.MyBlazorApp.Shared.TodoItem

  - Variable: MyBlazorAppWebsiteUrl
    Description: MyBlazorApp Website URL
    Scope: public
    Value: !GetAtt MyBlazorApp::Bucket.Outputs.WebsiteUrl
```
