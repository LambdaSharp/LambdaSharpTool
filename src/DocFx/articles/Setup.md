---
title: Setup LambdaSharp
description: Getting started with LambdaSharp
keywords: video, tutorial, getting started, overview, setup, install
---

![λ#](~/images/LambdaSharpLogo.png)

# Setup LambdaSharp

## Step 0: Prerequisite

To get started, make sure you have signed-up for an AWS account and downloaded the required tools.

1. [AWS Account](https://portal.aws.amazon.com/billing/signup#/start)
1. [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-getting-started.html)
1. [.NET Core 2.1+](https://www.microsoft.com/net/download)


## Step 1: Installing LambdaSharp CLI

The LambdaSharp CLI can be installed as a global `dotnet` tool by running the `dotnet` tool installation command:

__Using PowerShell/Bash:__
```bash
dotnet tool install -g LambdaSharp.Tool
```

Alternatively, for LambdaSharp contributors, the CLI can be setup using the [GitHub repository](https://github.com/LambdaSharp/LambdaSharpTool). See the LambdaSharp contributor installation instructions below.

Once installed, validate that the command works by running it.

__Using PowerShell/Bash:__
```bash
lash
```

The following text should appear (or similar):
```
LambdaSharp CLI (v0.7.0)

Project Home: https://github.com/LambdaSharp/LambdaSharpTool

Usage: lash [options] [command]

Options:
  -?|-h|--help  Show help information

Commands:
  build         Build LambdaSharp module
  deploy        Deploy LambdaSharp module
  encrypt       Encrypt Value
  info          Show LambdaSharp information
  init          Create or update a LambdaSharp deployment tier
  list          List deployed LambdaSharp modules
  new           Create new LambdaSharp module, function, or resource
  publish       Publish LambdaSharp module
  tier          Update settings module in LambdaSharp tier
  util          Miscellaneous AWS utilities

Run 'lash [command] --help' for more information about a command.
```

## Step 2: Initialize a LambdaSharp Deployment Tier

The LambdaSharp CLI must initialize a deployment tier before it can be used. The initialization creates the needed resources for deploying LambdaSharp modules. Multiple deployment tiers can be created by using the `--tier` option (e.g. `Test`, `Stage`, `Prod`, etc.), which prefixes the deployment tier name.

A deployment tier can be initialized with or without LambdaSharp.Core services. The LambdaSharp.Core services provide functionality for tracking errors across deployed modules and are recommended for testing, staging, and production deployment tiers.

As a convenience, the `--quick-start` option streamlines the process of setting up a deployment tier and is recommended for personal work and demos.

__Using PowerShell/Bash:__
```bash
lash init --quick-start
```

The following text should appear (or similar):
```
LambdaSharp CLI (v0.7.0) - Create or update a LambdaSharp deployment tier
Creating LambdaSharp tier
=> Stack creation initiated for LambdaSharp-Core
CREATE_COMPLETE    AWS::CloudFormation::Stack    LambdaSharp-Core
CREATE_COMPLETE    AWS::S3::Bucket               DeploymentBucketResource
=> Stack creation finished
=> Checking API Gateway role

Done (finished: 8/15/2019 2:53:45 PM; duration: 00:00:33.2020715)
```

## (Optional) Enable LambdaSharp.Core Services

The LambdaSharp.Core services provide functionality for tracking errors across deployed modules and are recommended for testing, staging, and production deployment tiers. An existing deployment tier can easily be updated to enable or disable the LambdaSharp.Core services.

__Using PowerShell/Bash:__
```bash
lash init --core-services enabled
```

## (Optional) Define Environment Variables

The following environment variables are checked when their corresponding options are omitted from the LambdaSharp command line.
* `LAMBDASHARP_TIER`: Replaces the need for the `--tier` option.
* `AWS_PROFILE`: Replaces the need for the `--aws-profile` option.

__Using PowerShell:__
```powershell
New-Variable -Name LAMBDASHARP_TIER -Value Sandbox
```

__Using Bash:__
```bash
export LAMBDASHARP_TIER=Sandbox
```

## (Optional) Customize LambdaSharp Core Settings

The following LambdaSharp Core module settings can be adjusted in the AWS console by updating the deployed CloudFormation stack.

|Parameter|Description|Default|
|---|---|---|
|`LoggingStreamRetentionPeriod`|How long logging stream entries are kept before they are lost|24|
|`LoggingStreamShardCount`|Number of Kinesis shards for the logging streams|1|

## (Optional) Subscribe to `ErrorReportTopic` Topic

The LambdaSharp Core module analyzes the output of all deployed functions. When an issue occurs, the Core module sends a notification on the SNS `ErrorReportTopic`.

## (Optional) Setup Rollbar Integration

The LambdaSharp Core module can optionally be configured to send errors and warnings to [Rollbar](https://rollbar.com/). To enable this functionality, the LambdaSharp Core module needs the _read_ and _write_ access tokens for the account, which can be found in the _Account Settings_ page.

The LambdaSharp Core module expects the access tokens to be encrypted, which can easily be done with the [`lash encrypt`](~/cli/Tool-Encrypt.md) command.

|Parameter|Description|Default|
|---|---|---|
|`RollbarReadAccessToken`|Account-level token for read operations|""|
|`RollbarWriteAccessToken`|Account-level token for write operations|""|
|`RollbarProjectPrefix`|Optional prefix when creating Rollbar projects|""|

# For LambdaSharp Contributors: Installing LambdaSharp from GitHub

LambdaSharp is distributed as [GitHub repository](https://github.com/LambdaSharp/LambdaSharpTool). Switch to your preferred folder for Git projects and create a clone of the LambdaSharp repository.

__Using PowerShell/Bash:__
```bash
git clone https://github.com/LambdaSharp/LambdaSharpTool.git
```

Define the `LAMBDASHARP` environment variable to point to the folder of the `LambdaSharpTool` clone. Furthermore, define `lash` as an alias to invoke the LambdaSharp CLI. The following script assumes LambdaSharp was cloned into the `/Repos/LambdaSharpTool` directory.

__Using PowerShell:__
```powershell
New-Variable -Name LAMBDASHARP -Value \Repos\LambdaSharpTool
function lash {
  dotnet run -p $LAMBDASHARP\src\LambdaSharp.Tool\LambdaSharp.Tool.csproj -- $args
}
```

__Using Bash:__
```bash
export LAMBDASHARP=/Repos/LambdaSharpTool
alias lash="dotnet run -p $LAMBDASHARP/src/LambdaSharp.Tool/LambdaSharp.Tool.csproj --"
```

__IMPORTANT:__ make sure to always use your  `lash` alias instead of the `lash` command.

In addition, you need to run `Scripts/set-lash-version.sh` to set required environment variables for building the project file. This script sets the `LAMBDASHARP_VERSION_PREFIX`, `LAMBDASHARP_VERSION_SUFFIX`, and `LAMBDASHARP_VERSION` environment variables.