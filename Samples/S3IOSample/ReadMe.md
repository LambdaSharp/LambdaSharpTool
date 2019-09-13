![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp `LambdaSharp.S3.IO` Module

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Capabilities

The `LambdaSharp.S3.IO` module contains resource type definitions to make it easier to work with S3 buckets.
* `LambdaSharp::S3::EmptyBucket`: This resource does nothing on creation or update, but when deleted it empties the contents of the associated S3 bucket.
* `LambdaSharp::S3::WriteJson`: This resource is used to write a JSON file to an S3 bucket using information.
* `LambdaSharp::S3::Unzip`: This resource unzips an archive into an S3 bucket.

To access these additional resource types, the `LambdaSharp.S3.IO` module must be referenced in the `Using` section of the module.

## Module Definition

The following module definition does the following:
1. It references the `LambdaSharp.S3.IO` to import its definitions.
1. It creates a new bucket.
1. It then copies the contents of the `WebsiteContents` package to it, which include the `index.html` and `error.html` files.
1. It then writes the module parameters to a JSON file in the S3 bucket so that the values can be loaded by the HTML files.
1. Finally, it registers the bucket with the `LambdaSharp::S3::EmptyBucket` so that the bucket is emptied automatically when the module is torn down.

```yaml
Module: Sample.S3.IO
Description: Showcase how to write files to an S3 bucket
Using:

  - Module: LambdaSharp.S3.IO@lambdasharp

Items:

  # Get site configuration settings
  - Parameter: Title
    Description: Website title
    Section: Website Settings
    Label: Website Title

  - Parameter: Message
    Description: Website message
    Section: Website Settings
    Label: Website Message

  # Write the site configuration settings to a JSON file in the S3 bucket
  - Resource: WriteWebsiteConfigJson
    Type: LambdaSharp::S3::WriteJson
    Properties:
      Bucket: !Ref WebsiteBucket
      Key: config.json
      Contents:
        title: !Ref Title
        message: !Ref Message

  # Create S3 bucket, make it publicly accessible, and register it for automatic emptying
  - Resource: WebsiteBucket
    Type: AWS::S3::Bucket
    Allow: ReadWrite
    Properties:
      AccessControl: PublicRead
      WebsiteConfiguration:
        IndexDocument: index.html
        ErrorDocument: error.html

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
  - Package: WebsiteContents
    Description: Package of web site files
    Files: assets/

  - Resource: UnzipWebsiteContents
    Type: LambdaSharp::S3::Unzip
    Properties:
      SourceBucket: !Ref DeploymentBucketName
      SourceKey: !Ref WebsiteContents
      DestinationBucket: !Ref WebsiteBucket
      DestinationKey: ""

  - Variable: WebsiteUrl
    Description: Website URL
    Scope: public
    Value: !GetAtt WebsiteBucket.WebsiteURL
```
