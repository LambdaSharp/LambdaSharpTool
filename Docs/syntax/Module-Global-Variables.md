---
title: Global Variables Reference - Module
description: LambdaSharp module global variables reference
keywords: module, global, variable, reference, syntax, yaml, cloudformation
---
# LambdaSharp Module - Global Variables

LambdaSharp modules have variables and resources defined implicitly as part of their definition.

## Module Variables

|Variable                      |Type                           |Definition                                    |
|------------------------------|-------------------------------|----------------------------------------------|
|`Module::DeadLetterQueue`     |Arn&lt;AWS::SQS::Queue&gt;     |Deadletter queue for failed messages
|`Module::FullName`            |String                         |Module namespace and name (e.g. `Namespace.Name`)
|`Module::Id`                  |String                         |CloudFormation stack name
|`Module::Info`                |String                         |Module full name, version, and origin
|`Module::LoggingStream`       |Arn&lt;AWS::Kinesis::Stream&gt;|Kinesis logging stream used by Lambda function logs subscription
|`Module::LoggingStreamRole`   |Arn&lt;AWS::IAM::Role&gt;      |IAM Role for used by Lambda function log subscription
|`Module::Name`                |String                         |Module name
|`Module::Namespace`           |String                         |Module namespace
|`Module::Role`                |Arn&lt;AWS::IAM::Role&gt;      |IAM Role used by all Lambda functions in the module
|`Module::RootId`              |String                         |Root CloudFormation stack when the module is nested; empty string otherwise
|`Module::Version`             |String                         |Module version


## Deployment Variables

|Variable                           |Type      |Definition                                    |
|-----------------------------------|----------|----------------------------------------------|
|`Deployment::BucketName`           |String    |S3 Bucket name from which the module is being deployed from.
|`Deployment::Tier`                 |String    |Deployment tier name. Empty string for default deployment tier.
|`Deployment::TierLowercase`        |String    |Deployment tier name in lowercase characters. Empty string for default deployment tier.
|`Deployment::TierPrefix`           |String    |Deployment tier prefix used to isolate resources. Empty string for default deployment tier.
|`Deployment::TierPrefixLowercase`  |String    |Deployment tier prefix in lowercase characters. Used by resources that require only lowercase characters (e.g. S3 buckets, domain names). Empty string for default deployment tier.


**NOTE:**
Some global variables are dependent on `UseCoreServices` and may not have a value otherwise.
* `Module::DeadLetterQueue` becomes `AWS::NoValue` when `UseCoreServices` is `true`.
* `Module::LoggingStream` becomes `AWS::NoValue` when `UseCoreServices` is `true`.
* `Module::LoggingStreamRole` becomes `AWS::NoValue` when `UseCoreServices` is `true`.

## Module Conditions
The following conditions are defined in each module.
* `XRayIsEnabled` is `true` when X-Ray tracing is enabled for the module.
* `UseCoreServices` is `true` when Core Services are enabled for the module.

## Module REST API Variables

The following resources and variables are defined when a module contains a function that uses an API Gateway source. Otherwise, these resources and variables are not defined.

|Variable                      |Type                          |Definition                                    |
|------------------------------|------------------------------|----------------------------------------------|
|`Module::RestApi`             |AWS::ApiGateway::RestApi      |REST API resource
|`Module::RestApi::CorsOrigin` |String                        |CORS Origin for REST API; is `!Ref AWS::NoValue` when not set
|`Module::RestApi::Deployment` |AWS::ApiGateway::Deployment   |Deployment for the REST API; this resource changes whenever a REST API resource or method is modified
|`Module::RestApi::DomainName` |String                        |Domain name of theREST API
|`Module::RestApi::Stage`      |AWS::ApiGateway::Stage        |Stage for deploying REST API; the stage name is always `LATEST`
|`Module::RestApi::Url`        |String                        |URL of the REST API

## Module WebSocket API Variables

The following resources and variables are defined when a module contains a function that uses an API Gateway V2 source. Otherwise, these resources and variables are not defined.

|Variable                      |Type                            |Definition                                    |
|------------------------------|--------------------------------|----------------------------------------------|
|`Module::WebSocket`             |AWS::ApiGatewayV2::Api        |WebSocket API resource
|`Module::WebSocket::Deployment` |AWS::ApiGatewayV2::Deployment |Deployment for the WebSocket API; this resource changes whenever an WebSocket API resource or method is modified
|`Module::WebSocket::DomainName` |String                        |Domain name of the WebSocket API
|`Module::WebSocket::Stage`      |AWS::ApiGatewayV2::Stage      |Stage for deploying WebSocket API; the stage name is always `LATEST`
|`Module::WebSocket::Url`        |String                        |URL of the WebSocket API

## Module Deployment Parameters

The following parameters are set by LambdaSharp CLI when deploying a module.

|Variable                      |Type                           |Definition                                    |
|------------------------------|-------------------------------|----------------------------------------------|
|`DeploymentBucketName`        |String                         |S3 Bucket name from which the module is being deployed from
|`DeploymentChecksum`          |String                         |Module checksum; changes whenever the module definition changes
|`DeploymentPrefix`            |String                         |Deployment tier prefix used to isolate resources
|`DeploymentPrefixLowercase`   |String                         |Deployment tier prefix in lowercase characters; used by resources that require only lowercase characters (e.g. S3 buckets)
