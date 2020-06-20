---
title: LambdaSharp CLI Tier Command - Check Deployment Tier Version
description: Check the version of the LambdaSharp deployment tier
keywords: cli, lambda, cloudformation, version, tier
---
# Show or Check Deployment Tier Version

The `tier version` command is used to show the version of the deployment tier.

The `--min-version` option is used to compare the specified version against the deployment tier version. If the value of the deployment tier version is equal or greater, the process exits with code 0. Otherwise, it exits with code 1. This result can be used to check if `lash init` needs to be run to upgrade a deployment tier.

## Options

<dl>

<dt><code>--min-version &lt;VERSION&gt;</code></dt>
<dd>

(optional) Minimum expected version
</dd>

<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>

(optional) Name of deployment tier (default: <code>LAMBDASHARP_TIER</code> environment variable)
</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS profile from the AWS credentials file
</dd>

<dt><code>--aws-region &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS region (default: read from AWS profile)
</dd>

<dt><code>--verbose|-V[:&lt;LEVEL&gt;]</code></dt>
<dd>

(optional) Show verbose output (0=Quiet, 1=Normal, 2=Detailed, 3=Exceptions; Normal if LEVEL is omitted)
</dd>

<dt><code>--no-ansi</code></dt>
<dd>

(optional) Disable colored ANSI terminal output
</dd>

<dt><code>--quiet</code></dt>
<dd>

(optional) Don't show banner or execution time
</dd>

</dl>

## Examples

### Show Deployment Tier Version

```bash
lash tier version
```

Output:
```
LambdaSharp CLI (v0.8.0.3) - Check Tier Version
Tier Version: 0.8.0.3

Done (finished: 6/18/2020 9:28:34 PM; duration: 00:00:00.9270535)
```

### Check if Deployment Tier Version is Equal or Greater to 0.8.0.5

__Using PowerShell/Bash:__
```bash
lash tier version --min-version 0.8.0.5
```

Output:
```
LambdaSharp CLI (v0.8.0.3) - Check Tier Version
Tier Version: 0.8.0.3 [ExitCode: 1]

Done (finished: 6/18/2020 9:29:21 PM; duration: 00:00:00.9652105)
```

### Check if Deployment Tier Version is Equal or Greater to 0.8.0.5 without Output

__Using PowerShell/Bash:__
```bash
lash tier version --min-version 0.8.0.5 --quiet
echo $?
```

Output:
```
1
```
