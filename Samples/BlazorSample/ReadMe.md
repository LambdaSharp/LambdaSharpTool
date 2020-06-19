![λ#](../../Docs/images/LambdaSharpLogo.png)

# LambdaSharp ASP.NET Core Blazor WebAssembly Sample

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

> **NOTE:** The [.NET Core SDK version 3.1.201 or later](https://dotnet.microsoft.com/download/dotnet-core/3.1) is required to use the 3.2 Preview Blazor WebAssembly template. Confirm the installed .NET Core SDK version by running `dotnet --version` in a command shell.

Make sure to check out the [Get started with ASP.NET Core Blazor WebAssembly](https://docs.microsoft.com/en-us/aspnet/core/blazor/get-started?view=aspnetcore-3.1&tabs=visual-studio-code) page to get started.


## Module Definition

The following module definition does the following:
1. It references the `LambdaSharp.S3.IO` to import its definitions.
1. It creates a new bucket.
1. It compiles the ASP.NET Core Blazor WebAssembly project and then copies the contents of the `WebsiteContents` package to it, which include the `index.html` and `error.html` files.
1. It then writes the module parameters to a JSON file in the S3 bucket so that the values can be loaded by the HTML files.
1. Finally, it registers the bucket with the `LambdaSharp::S3::EmptyBucket` so that the bucket is emptied automatically when the module is torn down.

```yaml
Module: Sample.BlazorWebAssembly
Description: A sample module showing how to deploy a ASP.NET Core Blazor WebAssembly website
Using:
  - Module: LambdaSharp.S3.IO@lambdasharp

Items:

  # Build the ASP.NET Core Blazor application and compress it into a zip package
  - Package: WebsiteContents
    Build: dotnet publish -c Release MyBlazorApp -p:BlazorEnableCompression=false
    Files: MyBlazorApp/bin/Release/netstandard2.1/publish/wwwroot/

  # Create S3 bucket, make it publicly accessible, and register it for automatic emptying
  - Resource: WebsiteBucket
    Type: AWS::S3::Bucket
    Allow: ReadWrite
    Properties:
      AccessControl: PublicRead
      WebsiteConfiguration:
        IndexDocument: index.html
        ErrorDocument: index.html

  - Resource: BucketPolicy
    Type: AWS::S3::BucketPolicy
    Properties:
      PolicyDocument:
        Id: WebsiteBucket
        Version: 2012-10-17
        Statement:
          - Sid: PublicReadForGetBucketObjects
            Effect: Allow
            Principal: '*'
            Action: s3:GetObject
            Resource: !Sub "arn:aws:s3:::${WebsiteBucket}/*"
      Bucket: !Ref WebsiteBucket

  - Resource: EmptyBucket
    Type: LambdaSharp::S3::EmptyBucket
    Properties:
      Bucket: !Ref WebsiteBucket

  # Upload the HTML assets and copy them to the bucket
  - Resource: UnzipWebsiteContents
    Type: LambdaSharp::S3::Unzip
    Properties:
      SourceBucket: !Ref Deployment::BucketName
      SourceKey: !Ref WebsiteContents
      DestinationBucket: !Ref WebsiteBucket
      DestinationKey: ""
      Encoding: GZIP

  - Variable: WebsiteUrl
    Description: Website URL
    Scope: public
    Value: !GetAtt WebsiteBucket.WebsiteURL
```
