![λ#](../Docs/LambdaSharp_v2_small.png)

# Setup LambdaSharp Tool & Environment

## Step 1: Installing λ# Tool

As of v0.4, the λ# tool can be installed as a global `dotnet` tool. Simply run the `dotnet` tool installation command:

__Using PowerShell/Bash:__
```bash
dotnet tool install -g MindTouch.LambdaSharp.Tool --version 0.4
```

Alternatively, for λ# contributors, the tool can be setup using the [GitHub repository](https://github.com/LambdaSharp/LambdaSharpTool). See the λ# contributor installation instructions below.

Once installed, validate that the command works by running it.

__Using Powershell/Bash:__
```bash
dotnet lash
```

The following text should appear (or similar):
```
MindTouch LambdaSharp Tool (v0.4)

Project Home: https://github.com/LambdaSharp/LambdaSharpTool

Usage: MindTouch.LambdaSharp.Tool [options] [command]

Options:
  -?|-h|--help  Show help information

Commands:
  build         Build LambdaSharp module
  config        Configure LambdaSharp environment
  deploy        Deploy LambdaSharp module
  encrypt       Encrypt with Default LambdaSharp Secrets Key
  info          Show LambdaSharp settings
  list          List LambdaSharp modules
  new           Create new LambdaSharp module or function
  publish       Publish LambdaSharp module
  setup         Setup LambdaSharp environment

Run 'MindTouch.LambdaSharp.Tool [command] --help' for more information about a command.
```

## Step 2: Configure λ# Tool

Before the λ# tool can be used, it must be configured. The configuration step optionally creates needed resources for deploying λ# modules and captures deployment preferences.

__Using Powershell/Bash:__
```bash
dotnet lash config
```

The λ# tool can be configured for multiple tool profiles using the `--tool-profile` option. When omitted, the _Default_ tool profile is assumed. The λ# tool configuration is stored in [AWS System Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-paramstore.html), so that it can be shared across teams on the same AWS account.

## Step 3: Setup λ# Environment

λ# must be setup for each deployment tier (e.g. `Test`, `Stage`, `Prod`, etc.). Once setup, λ# modules can be deployed.

__Using Powershell/Bash:__
```bash
dotnet lash setup --tier Demo
```

__NOTE:__ This step must to be repeated for each deployment tier (e.g. `Test`, `Stage`, `Prod`, etc.).

Run the `list` command to confirm that all λ# modules were deployed successfully:

__Using Powershell/Bash:__
```bash
dotnet lash list --tier Demo
```

The following text should appear (or similar):
```
MindTouch LambdaSharp Tool (v0.4) - List deployed LambdaSharp modules

MODULE                        STATUS                DATE
LambdaSharp                   [UPDATE_COMPLETE]     2018-10-25 13:57:12
LambdaSharpRegistrar          [UPDATE_COMPLETE]     2018-10-25 13:58:56
LambdaSharpS3Subscriber       [UPDATE_COMPLETE]     2018-10-25 13:59:47
LambdaSharpS3PackageLoader    [UPDATE_COMPLETE]     2018-10-25 14:00:20

Found 4 modules for deployment tier 'Demo'
```

## Optional: Use λ# Environment Variable

The following environment variables are checked when their corresponding options are omitted from the λ# command line.
* `LAMBDASHARP_TIER`: Replaces the need for the `--tier` option.
* `LAMBDASHARP_PROFILE`: Replaces the need for the `--tool-profile` option.

__Using PowerShell:__
```powershell
New-Variable -Name LAMBDASHARP_TIER -Value Demo
```

__Using Bash:__
```bash
export LAMBDASHARP_TIER=Demo
```

## Optional: Customize LambdaSharp Settings

|Parameter|Description|Default|
|---|---|---|
|`LoggingStreamRetentionPeriod`|How long logging stream entries are kept before they are lost|24|
|`DefaultSecretKeyRotationEnabled`|Rotate KMS key automatically every 365 days|false|

## Optional: Customize LambdaSharpRegistrar Settings

The registrar is responsible for tracking the registration of all deployed modules and functions. Once registered, the registrar processes the CloudWatch logs of all functions and sends errors/warnings to the SNS `ErrorReportTopic`.

### Rollbar Settings

The registrar can optionally be configured to send errors and warnings to [Rollbar](https://rollbar.com/). To enable this functionality, the registrar needs the _read_ and _write_ access tokens for the account, which can be found in the _Account Settings_ page.

The registrar expects the access tokens to be encrypted, which can easily be done with the [`dotnet lash encrypt`](../src/MindTouch.LambdaSharp.Tool/Docs/Tool-Encrypt.md) command.

settings for the registrar have to be updated with the following va

|Parameter|Description|Default|
|---|---|---|
|`RollbarReadAccessToken`|Account-level token for read operations|""|
|`RollbarWriteAccessToken`|Account-level token for write operations|""|
|`RollbarProjectPrefix`|Optional prefix when creating Rollbar projects|""|

### Registration Table Settings

The registrar uses a DynamoDB table to store module and functions registrations. For larger λ# deployments it may be necessary to provision more read/write capacity.

|Parameter|Description|Default|
|---|---|---|
|`RegistrationTableReadCapacity`|Provisioned read capacity for registrations table|1|
|`RegistrationTableWriteCapacity`|Provisioned write capacity for registrations table|1|

## For λ# Contributors: Installing λ# Tool from GitHub

The λ# tool is distributed as [GitHub repository](https://github.com/LambdaSharp/LambdaSharpTool). Switch to your preferred folder for Git projects and create a clone of the λ# tool.

__Using Powershell/Bash:__
```bash
git clone https://github.com/LambdaSharp/LambdaSharpTool.git
```

Define the `LAMBDASHARP` environment variable to point to the folder of the `LambdaSharpTool` clone. Furthermore, define `lash` as an alias to invoke the λ# tool. The following script assumes the λ# tool was cloned into the `/Repos/LambdaSharpTool` directory.

__Using PowerShell:__
```powershell
New-Variable -Name LAMBDASHARP -Value \Repos\LambdaSharpTool
function lash {
  dotnet run -p $LAMBDASHARP\src\MindTouch.LambdaSharp.Tool\MindTouch.LambdaSharp.Tool.csproj -- $args
}
```

__Using Bash:__
```bash
export LAMBDASHARP=/Repos/LambdaSharpTool
alias lash="dotnet run -p $LAMBDASHARP/src/MindTouch.LambdaSharp.Tool/MindTouch.LambdaSharp.Tool.csproj --"
```

__IMPORTANT:__ make sure to always use your  `lash` alias instead of the `dotnet lash` command.
