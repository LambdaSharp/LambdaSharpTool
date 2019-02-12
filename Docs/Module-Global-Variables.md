![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Global Variables

λ# modules have variables and resources defined implicitly as part of their definition.

## Module Variables

|Variable                      |Type                           |Definition                                    |
|------------------------------|-------------------------------|----------------------------------------------|
|`Module::DeadLetterQueue`     |Arn&lt;AWS::SQS::Queue&gt;     |Deadletter queue for failed messages
|`Module::DefaultSecretKey`    |Arn&lt;AWS::KMS::Key&gt;       |Default encryption key for deployment tier
|`Module::FullName`            |String                         |Module owner and name (e.g. `Owner.Name`)
|`Module::Id`                  |String                         |CloudFormation stack name
|`Module::LoggingStream`       |Arn&lt;AWS::Kinesis::Stream&gt;|Kinesis logging stream used by Lambda function logs subscription
|`Module::LoggingStreamRole`   |Arn&lt;AWS::IAM::Role&gt;      |IAM Role for used by Lambda function log subscription
|`Module::Name`                |String                         |Module name
|`Module::Owner`               |String                         |Module owner
|`Module::Role`                |Arn&lt;AWS::IAM::Role&gt;      |IAM Role used by all Lambda functions in the module
|`Module::Version`             |String                         |Module version

## Module API Gateway Variables

The following resources and variables are defined when a module contains a function that uses an API Gateway source. Otherwise, these resources and variables are not defined.

|Variable                      |Type                          |Definition                                    |
|------------------------------|------------------------------|----------------------------------------------|
|`Module::RestApi`             |AWS::ApiGateway::RestApi      |API Gateway REST API resource
|`Module::RestApi::Account`    |AWS::ApiGateway::Account      |Account for the API Gateway REST API
|`Module::RestApi::Deployment` |AWS::ApiGateway::Deployment   |Deployment for the API Gateway REST API; this resource changes whenever an API resource or method is modified
|`Module::RestApi::Role`       |Arn&lt;AWS::IAM::Role&gt;     |IAM Role used by the API Gateway REST API Account
|`Module::RestApi::Stage`      |AWS::ApiGateway::Stage        |Stage for deploying API Gateway REST API; the stage name is always `LATEST`
|`Module::RestApi::Url`        |String                        |URL of the API Gateway REST API

## Module Deployment Variables

The following variables are set by λ# CLI when deploying a module.

|Variable                      |Type                           |Definition                                    |
|------------------------------|-------------------------------|----------------------------------------------|
|`DeploymentBucketName`        |String                         |S3 Bucket name from which the module is being deployed from
|`DeploymentChecksum`          |String                         |Module checksum; changes whenever the module definition changes
|`DeploymentRootId`            |String                         |Root CloudFormation stack when the module is nested; empty string otherwise
|`DeploymentPrefix`            |String                         |Deployment tier prefix used to isolate resources
|`DeploymentPrefixLowercase`   |String                         |Deployment tier prefix in lowercase characters; used by resources that require only lowercase characters (e.g. S3 buckets)
