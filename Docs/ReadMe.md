![λ#](../Docs/LambdaSharp_v2_small.png)

# Setup LambdaSharp

## Step 0: Prerequisite

To get started, make sure you have signed-up for an AWS account and downloaded the required tools.

1. [AWS Account](https://portal.aws.amazon.com/billing/signup#/start)
1. [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-getting-started.html)
1. [.NET Core 2.1+](https://www.microsoft.com/net/download)


## Step 1: Installing λ# CLI

The λ# CLI can be installed as a global `dotnet` tool by running the `dotnet` tool installation command:

__Using PowerShell/Bash:__
```bash
dotnet tool install -g LambdaSharp.Tool
```

Alternatively, for λ# contributors, the CLI can be setup using the [GitHub repository](https://github.com/LambdaSharp/LambdaSharpTool). See the λ# contributor installation instructions below.

Once installed, validate that the command works by running it.

__Using PowerShell/Bash:__
```bash
lash
```

The following text should appear (or similar):
```
LambdaSharp CLI (v0.5)

Project Home: https://github.com/LambdaSharp/LambdaSharpTool

Usage: lash [options] [command]

Options:
  -?|-h|--help  Show help information

Commands:
  build         Build LambdaSharp module
  config        Configure LambdaSharp CLI
  deploy        Deploy LambdaSharp module
  encrypt       Encrypt Value
  info          Show LambdaSharp information
  init          Initialize LambdaSharp deployment tier
  list          List deployed LambdaSharp modules
  new           Create new LambdaSharp module or function
  publish       Publish LambdaSharp module
  util          Miscellaneous AWS utilities

Run 'lash [command] --help' for more information about a command.
```

## Step 2: Configure λ# CLI

The λ# CLI must be configured before it can be used. The configuration step optionally creates needed resources for deploying λ# modules and captures deployment preferences.

__Using PowerShell/Bash:__
```bash
lash config
```

The λ# CLI can be configured for multiple CLI profiles using the `--cli-profile` option. When omitted, the _Default_ CLI profile is assumed. The λ# CLI configuration options are stored in [AWS System Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-paramstore.html), so they can be shared across teams on the same AWS account.

## Step 3: Initialize λ# Deployment Tier

The λ# CLI must initialize each deployment tier (e.g. `Test`, `Stage`, `Prod`, etc.) with the λ# Core module before additional modules can be deployed.

__Using PowerShell/Bash:__
```bash
lash init --tier Sandbox
```

__NOTE:__ This step must to be repeated for each deployment tier (e.g. `Test`, `Stage`, `Prod`, etc.).

Run the `list` command to confirm that all λ# modules were deployed successfully:

__Using PowerShell/Bash:__
```bash
lash list --tier Sandbox
```

The following text should appear (or similar):
```
LambdaSharp CLI (v0.5) - List deployed LambdaSharp modules

MODULE               STATUS             DATE
LambdaSharp-Core     [CREATE_COMPLETE]  2019-01-15 20:40:53

Found 1 modules for deployment tier 'Default'

Done (finished: 1/17/2019 3:09:22 PM; duration: 00:00:03.5455994)
```

## Optional: Define Environment Variables

The following environment variables are checked when their corresponding options are omitted from the λ# command line.
* `LAMBDASHARP_TIER`: Replaces the need for the `--tier` option.
* `LAMBDASHARP_PROFILE`: Replaces the need for the `--cli-profile` option.

__Using PowerShell:__
```powershell
New-Variable -Name LAMBDASHARP_TIER -Value Sandbox
```

__Using Bash:__
```bash
export LAMBDASHARP_TIER=Sandbox
```

## Optional: Customize LambdaSharp Core Settings

The following λ# Core module settings can be adjusted in the AWS console by updating the deployed CloudFormation stack.

|Parameter|Description|Default|
|---|---|---|
|`LoggingStreamRetentionPeriod`|How long logging stream entries are kept before they are lost|24|
|`DefaultSecretKeyRotationEnabled`|Rotate KMS key automatically every 365 days|false|

## Optional: Add S3 Module Locations

The following λ# CLI settings can be adjusted in the AWS console by accessing the Parameter Store in the Systems Manager.

|Parameter|Description|Default|
|---|---|---|
|`/LambdaSharpTool/${CLI Profile}/ModuleBucketNames`|Comma-separated list of S3 bucket names used by the CLI to find modules|`${DeploymentBucket},lambdasharp-${AWS::Region}`|

## Optional: Subscribe to `ErrorReportTopic` Topic

The λ# Core module analyzes the output of all deployed functions. When an issue occurs, the Core module sends a notification on the SNS `ErrorReportTopic`.

## Optional: Setup Rollbar Integration

The λ# Core module can optionally be configured to send errors and warnings to [Rollbar](https://rollbar.com/). To enable this functionality, the λ# Core module needs the _read_ and _write_ access tokens for the account, which can be found in the _Account Settings_ page.

The λ# Core module expects the access tokens to be encrypted, which can easily be done with the [`lash encrypt`](../src/LambdaSharp.Tool/Docs/Tool-Encrypt.md) command.

|Parameter|Description|Default|
|---|---|---|
|`RollbarReadAccessToken`|Account-level token for read operations|""|
|`RollbarWriteAccessToken`|Account-level token for write operations|""|
|`RollbarProjectPrefix`|Optional prefix when creating Rollbar projects|""|

# For λ# Contributors: Installing λ# from GitHub

λ# is distributed as [GitHub repository](https://github.com/LambdaSharp/LambdaSharpTool). Switch to your preferred folder for Git projects and create a clone of the λ# repository.

__Using PowerShell/Bash:__
```bash
git clone https://github.com/LambdaSharp/LambdaSharpTool.git
```

Define the `LAMBDASHARP` environment variable to point to the folder of the `LambdaSharpTool` clone. Furthermore, define `lash` as an alias to invoke the λ# CLI. The following script assumes λ# was cloned into the `/Repos/LambdaSharpTool` directory.

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
