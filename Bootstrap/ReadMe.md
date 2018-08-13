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
  dotnet run -p $LAMBDASHARP\src\MindTouch.LambdaSharp.Tool\MindTouch.LambdaSharp.Tool.csproj -- 
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
MindTouch LambdaSharp Tool (v0.2.0)
Project Home: https://github.com/LambdaSharp/LambdaSharpTool

Usage: MindTouch.LambdaSharp.Tool [options] [command]

Options:
  -?|-h|--help  Show help information

Commands:
  deploy        Deploy LambdaSharp module
  info          Show LambdaSharp settings
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
    $LAMBDASHARP\Bootstrap\LambdaSharp\Deploy.yml `
    $LAMBDASHARP\Bootstrap\LambdaSharpS3PackageLoader\Deploy.yml
```

__Using Bash:__
```bash
lash deploy \
    --tier Demo \
    $LAMBDASHARP/Bootstrap/LambdaSharp/Deploy.yml \
    $LAMBDASHARP/Bootstrap/LambdaSharpS3PackageLoader/Deploy.yml
```
