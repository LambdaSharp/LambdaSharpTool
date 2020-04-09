---
title: LambdaSharp CLI - Nuke Command
description: Delete a LambdaSharp deployment tier
keywords: cli, nuke, module, deployment, tier
---
# Delete Deployment Tier

The `nuke` command is used to delete all module deployments from a deployment tier, including `LambdaSharp.Core` and the deployment bucket.

The user will have to confirm the operation twice, including confirming the deployment tier name, unless the `--confirm-tier` or `--dryrun` options are used.

## Options

<dl>

<dt><code>--dryrun</code></dt>
<dd>

(optional) Show the result of the delete operation without deleting anything
</dd>

<dt><code>--confirm-tier &lt;NAME&gt;</code></dt>
<dd>

(optional) Confirm deployment tier name to skip confirmation prompts
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

</dl>


## Examples

### List all deployed modules

__Using PowerShell/Bash:__
```bash
lash nuke --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.7.0.13) - Delete a LambdaSharp deployment tier
=> Inspecting deployment tier Sandbox
=> Found 2 module deployments to delete

  LambdaSharp-S3-IO
  LambdaSharp-Core

|=> Confirm the deployment tier name to delete: Sandbox
|=> Proceed with deleting deployment tier 'SteveBv7' [y/N] y
...
```