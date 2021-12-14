---
title: LambdaSharp Video Tutorials
description: List of video tutorials to get started with LambdaSharp
keywords: video, tutorial, getting started, overview
---

# Video Tutorials

Subscribe to the [LambdaSharp YouTube channel](https://www.youtube.com/channel/UC9zH5HkC6dHvuFJR6_XZzFg) to stay up-to-date on the latest videos.

## Getting Started

It's easy to get started building Serverless .NET application on AWS with LambdaSharp. In in this 10 minute tutorial, we will install the LambdaSharp CLI, create a configuration for it, and a deployment tier for your LambdaSharp modules. Along the way, I will explain what resources are part of the configuration and deployment tier and what purpose they play.

<iframe width="560" height="315" src="https://www.youtube.com/embed/2N6mw8rObng" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## First Module

In this video, I'm going through the steps for creating your first Serverless .NET module for AWS using  LambdaSharp. The whole tutorial takes less than 10 minutes. Along the way, I also show how to use CloudFormation parameters to parametrize your module and show how they are resolved interactively at deployment time.

<iframe width="560" height="315" src="https://www.youtube.com/embed/35fyBngzUSs" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## AWS re:Invent 2020: Serverless .NET on AWS with LambdaSharp

Learn how you can build and deploy your serverless solution in minutes using LambdaSharp, an open-source CLI and framework for serverless .NET Core application development on AWS. The solution shares the same C# code for the backend and front end leveraging AWS Lambda functions and the Blazor WebAssembly framework. Finally, see a demonstration of how easy it is to integrate with Amazon CloudWatch Logs, metrics, and Amazon EventBridge.

<iframe width="560" height="315" src="https://www.youtube.com/embed/wN_0mQ7AUg8" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Serverless .NET Patterns: Service Composition

This presentation covers some of the serverless design patterns, such as CQRS (Command and Query Responsibility Separation), CloudFormation stacks, sharing of resources, nested vs. side-by-side composition, and then put it all together with some code samples found at: https://github.com/LambdaSharp/ServerlessPatterns-ServiceComposition

<iframe width="560" height="315" src="https://www.youtube.com/embed/P8o7ZI8XCRg" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Serverless .NET Patterns: Publishing and Sharing

This presentation dives into the details of how modules are published. It shows how artifacts from the build process are copied to the deployment bucket. As well as how to stage builds for validation, before publishing the approved artifacts. It is a deep-dive into one of the most critical features in LambdaSharp to ensure a safe development process for production environments. Code samples can be found at: https://github.com/LambdaSharp/ServerlessPatterns-PublishingAndSharing

<iframe width="560" height="315" src="https://www.youtube.com/embed/d7J0cyhCZUc" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Serverless .NET Patterns: Deployment Configuration

In this presentation, I cover configuration management for your serverless solutions. We dive into the various CloudFormation parameter types, as well as the parameter file format for LambdaSharp modules. We also explore--with code samples--how to read values from a JSON configuration file, the parameter store, and how to securely encode sensitive infrastructure information. Code samples can be found at: https://github.com/LambdaSharp/ServerlessPatterns-DeploymentConfiguration

<iframe width="560" height="315" src="https://www.youtube.com/embed/shVf1jjz83E" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Serverless .NET Patterns: Should you use Kinesis Stream, Firehose, or SQS?

In this presentation, I cover the differences between Kinesis Stream, Firehose, and SQS for event-driven architectures. We will build a sample application with each. Then we will combine them to get the best of both worlds using a scatter-gather pattern.

<iframe width="560" height="315" src="https://www.youtube.com/embed/4mybJ5G0S9Q" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Serverless .NET Patterns: DynamoDB for Fun and Glory!

In this presentation, I dive into DynamoDB, Amazon's serverless NoSQL database. DynamoDB is a powerful tool that comes with a steep learning curve, but offers many rewards to those willing to climb it. Until now, the API has also been difficult to use, but in the session I will showcase a new library that makes it both easy and safe for .NET developers to leverage DynamoDB in their applications. First, I cover the fundamental mechanics of DynamoDB and then write some actual code samples.

<iframe width="560" height="315" src="https://www.youtube.com/embed/qK804VcZTKo" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Serverless .NET Patterns: Writing Testable Lambda Business Logic

In this presentation, I show how to build a Lambda function from scratch with decoupled business logic. We use the `DependencyProvider` pattern to separate IO operations and make testing much easier. We have used this pattern for many years in our microservices and Lambda functions with great success.

<iframe width="560" height="315" src="https://www.youtube.com/embed/cf7Gy9wyFeA" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
