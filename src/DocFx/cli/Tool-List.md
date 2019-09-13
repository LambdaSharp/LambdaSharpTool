---
title: LambdaSharp CLI - List Command
description: List deployed LambdaSharp modules in a deployment tier
keywords: cli, list, module, deployment, tier
---
# List Deployed Modules

The `list` command is used to list all deployed modules on a deployment tier.

## Options

<dl>

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

### List all deployed modules

__Using PowerShell/Bash:__
```bash
lash list --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.6) - List deployed LambdaSharp modules

Found 2 modules for deployment tier 'Default'

NAME                 MODULE                   STATUS             DATE
LambdaSharp-Core     LambdaSharp.Core:0.6     UPDATE_COMPLETE    2019-04-05 10:36:49
LambdaSharp-S3-IO    LambdaSharp.S3.IO:0.6    UPDATE_COMPLETE    2019-04-05 10:37:19

Done (finished: 4/5/2019 3:36:10 PM; duration: 00:00:01.4137682)
```
