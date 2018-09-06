![λ#](../Docs/LambdaSharp_v2_small.png)

# Setup LambdaSharp Environment

Setting up the λ# environment is required for each deployment tier (e.g. `Test`, `Stage`, `Prod`, etc.).

## Setup λ# Tool

The λ# tool is distributed as [GitHub repository](https://github.com/LambdaSharp/LambdaSharpTool). Switch to your preferred folder for Git projects and create a clone of the λ# tool.

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

Once setup, validate that the command works by running:
```bash
lash
```

The following text should appear (or similar):
```
MindTouch LambdaSharp Tool (v0.3)

Project Home: https://github.com/LambdaSharp/LambdaSharpTool

Usage: MindTouch.LambdaSharp.Tool [options] [command]

Options:
  -?|-h|--help  Show help information

Commands:
  deploy        Deploy LambdaSharp module
  info          Show LambdaSharp settings
  list          List LambdaSharp modules
  new           Create new LambdaSharp asset

Run 'MindTouch.LambdaSharp.Tool [command] --help' for more information about a command.
```

## Bootstrap λ# Environment

The λ# environment requires an AWS account to be setup for each deployment tier (e.g. `Test`, `Stage`, `Prod`, etc.). Once setup, λ# modules can be deployed.

The following command creates the AWS resources needed to deploy λ# modules, such as a the deployment bucket, dead-letter queue, S3 package loader, etc. For the purpose of this tutorial, we use `Demo` as the new deployment tier name.

Ths step must to be repeated for each deployment tier (e.g. `Test`, `Stage`, `Prod`, etc.).

__Using Powershell:__
```powershell
lash deploy `
    --tier Demo `
    $LAMBDASHARP\Bootstrap\LambdaSharp\Module.yml `
    $LAMBDASHARP\Bootstrap\LambdaSharpS3PackageLoader\Module.yml `
    $LAMBDASHARP\Bootstrap\LambdaSharpS3Subscriber\Module.yml
```

__Using Bash:__
```bash
lash deploy \
    --tier Demo \
    $LAMBDASHARP/Bootstrap/LambdaSharp/Module.yml \
    $LAMBDASHARP/Bootstrap/LambdaSharpS3PackageLoader/Module.yml \
    $LAMBDASHARP/Bootstrap/LambdaSharpS3Subscriber/Module.yml
```

## Validate λ# Environment

Run the `list` command to confirm that all λ# modules were deployed successfully:

```bash
lash list --tier Demo
```

The following text should appear (or similar):
```
MindTouch LambdaSharp Tool (v0.3.0.0) - List LambdaSharp modules

MODULE                        STATUS             DATE
LambdaSharp                   [CREATE_COMPLETE]  2018-08-19 22:48:45
LambdaSharpS3PackageLoader    [CREATE_COMPLETE]  2018-08-22 01:06:02
LambdaSharpS3Subscriber       [CREATE_COMPLETE]  2018-09-03 15:46:36

Found 3 modules for deployment tier 'Demo'
```
## Use `LAMBDASHARPTIER` Environment Variable

You can omit the `--tier` option from the λ# tool command line if you define the `LAMBDASHARPTIER` environment variable instead.

__Using PowerShell:__
```powershell
New-Variable -Name LAMBDASHARPTIER -Value Demo
```

__Using Bash:__
```bash
export LAMBDASHARPTIER=Demo
```

Once `LAMBDASHARPTIER` is defined, the following command will produce the same result.
```bash
lash list
```
