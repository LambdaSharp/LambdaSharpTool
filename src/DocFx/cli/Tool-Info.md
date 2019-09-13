---
title: LambdaSharp CLI - Info Command
description: Show information about LambdaSharp deployment tier
keywords: cli, show, info, information, deployment, tier
---
# Show Information

The `info` command is used to show information about the current CLI profile, LambdaSharp environment, and installed command line tools.

## Options

<dl>

<dt><code>--show-sensitive</code></dt>
<dd>

(optional) Show sensitive information
</dd>

<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>

(optional) Name of deployment tier (default: <code>LAMBDASHARP_TIER</code> environment variable)
</dd>

<dt><code>--cli-profile|-C &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific LambdaSharp CLI profile (default: Default)
</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS profile from the AWS credentials file
</dd>

<dt><code>--verbose|-V[:&lt;LEVEL&gt;]</code></dt>
<dd>

(optional) Show verbose output (0=Quiet, 1=Normal, 2=Detailed, 3=Exceptions; Normal if LEVEL is omitted)
</dd>

<dt><code>--no-ansi</code></dt>
<dd>

Disable colored ANSI terminal output
</dd>

</dl>

## Examples

### Show information for Default profile and Sandbox tier

__Using PowerShell/Bash:__
```bash
lash info --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.5) - Show LambdaSharp information
LambdaSharp CLI
    Profile: Sandbox
    Version: 0.5
    Module Deployment S3 Bucket: lambdasharp-bucket-name
    Deployment Notifications Topic: arn:aws:sns:us-east-1:************:LambdaSharpTool-Sandbox-DeploymentNotificationTopicResource-QMM6DIP3K4N4
    Module S3 Buckets:
        - lambdasharp-bucket-name
        - lambdasharp-us-east-1
LambdaSharp Deployment Tier
    Name: Sandbox
    Version: 0.5
Git
    Branch: Docs
    SHA: ae537bc2214710eed89f5c3b5819d809c065856f
AWS
    Region: us-east-1
    Account Id: ************
Tools
    .NET Core CLI Version: 2.1.403
    Git CLI Version: 2.18.0.windows.1

Done (finished: 1/17/2019 4:27:54 PM; duration: 00:00:06.2172050)
```
