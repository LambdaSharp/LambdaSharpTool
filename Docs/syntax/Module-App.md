---
title: App Module Declaration - Module
description: LambdaSharp YAML syntax for apps
keywords: app, declaration, module, syntax, yaml, cloudformation
---
# App

The `App` declaration specifies a [Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) app for deployment. Each declaration is compiled and uploaded as part of the deployment process.

The Blazor app is supported by two nested stacks. The first one creates an S3 bucket to host the deployed app files using the [LambdaSharp.App.Bucket](~/Modules/LambdaSharp-App-Bucket.md) module. The second one creates a REST API to send messages and metrics to [Amazon CloudWatch](https://aws.amazon.com/cloudwatch/) and events to [Amazon EventBridge](https://aws.amazon.com/eventbridge/) using the [LambdaSharp.App.Api](~/Modules/LambdaSharp-App-Api.md). Within the app, use the [LambdaSharpAppClient](xref:LambdaSharp.App.LambdaSharpAppClient) singleton to connect to the REST API. The client is automatically initialized on startup with the app API URL and its matching API key. Alternatively, the [LambdaSharpAppClient](xref:LambdaSharp.App.LambdaSharpAppClient) instance can also be accessed using via the [ILogger](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-3.1) interface.

Use the [lash new app](~/cli/Tool-New-App.md) command to add the `App` declaration with a pre-configured project to the module. The new project can be launched with `dotnet run` from within the folder with the app _.csproj_ file. However, in this mode, the Blazor WebAssembly does not connect to app API since it is launched locally.

Modules with an `App` declaration have an additional `AppDeveloperMode` CloudFormation parameter. By default, `AppDeveloperMode` is set to `Disabled`. In this mode, the app API key is based on the CloudFormation stack identifier of the REST API and the assembly version identifier of the app. The CloudFormation stack identifier changes with every new deployment, while the assembly version identifier changes with every build. This ensures that every app deployment has a unique API key to secure communication with the app API. In addition, the web console only shows errors emitted by [LambdaSharpAppClient](xref:LambdaSharp.App.LambdaSharpAppClient). Warning and information logging is still sent to the app API, but not shown. Debug logging is neither shown, nor sent to the app API.

When `AppDeveloperMode` is `Enabled`, the app API key is only based on the CloudFormation stack identifier. In addition, the CORS origin attribute is always set to `*` to allow connections from any origin to make it easier to run the app on _localhost_ and still connect to the app API. Finally, all [LambdaSharpAppClient](xref:LambdaSharp.App.LambdaSharpAppClient) logging is shown in the web console and sent to the app API.

## Syntax

```yaml
App: String
Description: String
Project: String
LogRetentionInDays: Number or Expression
Pragmas:
  - PragmaDefinition
Api:
  RootPath: String or Expression
  CorsOrigin: String or Expression
  BurstLimit: Number or Expression
  RateLimit: Number or Expression
  EventSource: String or Expression
Bucket:
  CloudFrontOriginAccessIdentity: String or Expression
  ContentEncoding: String or Expression
Client:
  ApiUrl: String or Expression
AppSettings:
  String: Expression
```

## Properties

<dl>

<dt><code>Api</code></dt>
<dd>

The <code>Api</code> section specifies the API configuration for the app.

<i>Required</i>: No

The <code>Api</code> section has the following attributes:
<dl>

<dt><code>RootPath</code></dt>
<dd>

The <code>RootPath</code> attribute specifies the root path for app API. The root path must be a single path segment. When omitted, the default value is <code>".app"</code>.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>CorsOrigin</code></dt>
<dd>

The <code>CorsOrigin</code> attribute specifies the source URL that is allowed to invoke the app API. The value must be <em>http://</em> or <em>https://</em> followed by a valid domain name in lowercase letters, or <code>*</code> to allow any domain. When omitted, the default value is the website URL for the S3 app bucket.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>BurstLimit</code></dt>
<dd>

The <code>BurstLimit</code> attribute specifies the maximum number of requests per second to the app API over a short period of time. When omitted, the default value is 200.

<i>Required</i>: No

<i>Type</i>: Number
</dd>

<dt><code>RateLimit</code></dt>
<dd>

The <code>RateLimit</code> attribute specifies the maximum number of requests per second to the app API over a long period of time. When omitted, the default value is 100.

<i>Required</i>: No

<i>Type</i>: Number
</dd>

<dt><code>EventSource</code></dt>
<dd>

The <code>EventSource</code> attribute specifies a 'Source' property override for app events. When empty, the 'Source' property is set by the app request. When omitted, the default value is <code>!Sub "Module:${Module::FullName}"</code>.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>
</dd>

<dt><code>AppSettings</code></dt>
<dd>

The <code>AppSettings</code> attribute specifies configuration values stored in the <code>appsettings.Production.json</code> file. This file is read during initialization of the app.

<i>Required</i>: No

<i>Type</i>: Map of key-value pair Expressions
</dd>

<dt><code>Bucket</code></dt>
<dd>

The <code>Bucket</code> section specifies the S3 bucket configuration for the app.

<i>Required</i>: No

The <code>Bucket</code> section has the following attributes:
<dl>

<dt><code>CloudFrontOriginAccessIdentity</code></dt>
<dd>

The <code>CloudFrontOriginAccessIdentity</code> attribute configures the S3 bucket for secure access from a CloudFront distribution. When omitted or left empty, the S3 bucket is configured as a public website instead.

<i>Required</i>: No

<i>Type</i>: String

</dd>

<dt><code>ContentEncoding</code></dt>
<dd>

The <code>ContentEncoding</code> attribute sets the content encoding to apply to all files copied from the app package. The value must be one of: <code>NONE</code>, <code>BROTLI</code>, <code>GZIP</code>, or <code>DEFAULT</code>. When omitted, the default value is <code>DEFAULT</code>.

<i>Required</i>: No

<i>Type</i>: String

The <code>ContentEncoding</code> attribute must have one of the following values:
<dl>

<dt><code>NONE</code></dt>
<dd>

No content encoding is performed and no <code>Content-Encoding</code> header is applied. Using no encoding is fastest to perform, but produces significantly larger files.
</dd>

<dt><code>BROTLI</code></dt>
<dd>

Content is encoded with <a href="https://en.wikipedia.org/wiki/Brotli">Brotli compression</a> using the optimal compression setting. Brotli compression takes longer to perform, but produces smaller files.

Note that Brotli encoding is only valid for <em>https://</em> connections.
</dd>

<dt><code>GZIP</code></dt>
<dd>

Content is encoded with <a href="https://en.wikipedia.org/wiki/Gzip">Gzip compression</a>. Gzip compression is faster than Brotli, but produces slightly larger files.
</dd>

<dt><code>DEFAULT</code></dt>
<dd>

The <code>DEFAULT</code> value defaults to <code>BROTLI</code> when an empty <code>CloudFrontOriginAccessIdentity</code> attribute is specified since CloudFront distributions are always served over <em>https://</em> connections. Otherwise, it defaults to <code>GZIP</code>, which is safe for connections over <em>https://</em> and <em>http://</em>.
</dd>

</dl>
</dd>

</dl>
</dd>

<dt><code>Client</code></dt>
<dd>

The <code>Client</code> section specifies the app client configuration.

<i>Required</i>: No

The <code>Client</code> section has the following attributes:
<dl>

<dt><code>ApiUrl</code></dt>
<dd>

The <code>ApiUrl</code> attribute specifies the URL used by the app client to connect to the app API. When omitted, the default value is the URL of the created API Gateway instance for the app.

<i>Required</i>: No

<i>Type</i>: String
</dd>

</dl>
</dd>

<dt><code>Description</code></dt>
<dd>

The <code>Description</code> attribute specifies the description of the app.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><code>LogRetentionInDays</code></dt>
<dd>

The <code>LogRetentionInDays</code> attribute specifies the number of days CloudWatch log entries are kept in the log group. When omitted, the default value is set by <code>Module::LogRetentionInDays</code>.

<i>Required</i>: No

<i>Type</i>: Number
</dd>

<dt><code>Pragmas</code></dt>
<dd>

The <code>Pragmas</code> section specifies directives that change the default compiler behavior.

<i>Required:</i> No

<i>Type:</i> List of [Pragma Definition](Module-App-Pragmas.md)
</dd>

<dt><code>Project</code></dt>
<dd>

The <code>Project</code> attribute specifies the relative path of the app project file or its folder.

<i>Required</i>: Conditional. By default, the .NET Core project file is expected to be located in a sub-folder of the module definition. The name of the sub-folder and project file are expected to match the app name. If that is not the case, then the <code>Project</code> attribute must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

</dl>


## Nested Resources

The `App` declaration adds two nested resources:
* `${AppName}::Bucket` is a CloudFormation stack using the [LambdaSharp.App.Bucket](~/Modules/LambdaSharp-App-Bucket.md) module.
* `${AppName}::Api` is a nested CloudFormation stack using the [LambdaSharp.App.Api](~/Modules/LambdaSharp-App-Api.md) module.

The nested stacks have output values that can be used to initialize other resources in the stack.


## Examples

### Creating a Blazor app

```yaml
- App: MyBlazorApp
```

### Creating a Blazor app with configuration values

```yaml
- App: MyBlazorApp
  AppSettings:
    Title: !Sub "Welcome from ${AWS::StackName}"
```

### Creating a Blazor app with a custom app API root path

```yaml
- App: MyBlazorApp
  Api:
    RootPath: app-api
  Client:
    ApiUrl: !Sub "https://${WebsiteCloudFront.DomainName}/${MyBlazorApp::Api.Outputs.RootPath}"
```

### Creating a Blazor app with CloudFront integration

The following app declaration configures the app API to allow access from any domain. Access could be restricted further using a custom domain, but not for dynamically generated CloudFront domain since the CloudFront distribution depends on the app API.

This example refers to the following resource declarations:
* `CloudFrontIdentity` is a resource of type `AWS::CloudFront::CloudFrontOriginAccessIdentity`
* `CloudFront` is a resource of type `AWS::CloudFront::Distribution`

```yaml
- App: MyBlazorApp
  Api:
    CorsOrigin: "*"
  Bucket:
    CloudFrontOriginAccessIdentity: !Ref CloudFrontIdentity
  Client:
    ApiUrl: !Sub "https://${CloudFront.DomainName}/${MyBlazorApp::Api.Outputs.RootPath}"
```

Add the following to the `Origins` section in the CloudFront distribution to proxy the app API:

```yaml
Origins:
  - Id: AppApi
    DomainName: !GetAtt MyBlazorApp::Api.Outputs.DomainName
    OriginPath: !GetAtt  MyBlazorApp::Api.Outputs.CloudFrontOriginPath
    CustomOriginConfig:
      HTTPSPort: 443
      OriginProtocolPolicy: https-only
```

Finally, add the following to the `CacheBehaviors` section in the CloudFront distribution to proxy to the app API:

```yaml
CacheBehaviors:
  - TargetOriginId: AppApi
    PathPattern: !GetAtt MyBlazorApp::Api.Outputs.CloudFrontPathPattern
    AllowedMethods: [ GET, HEAD, OPTIONS, PUT, PATCH, POST, DELETE  ]
    ForwardedValues:
      QueryString: false
      Headers:
        - X-Api-Key
    ViewerProtocolPolicy: https-only
```

