---
title: LambdaSharp CLI Tier Command - List Deployment Tiers
description: List all available deployment tiers
keywords: cli, lambda, cloudformation, version, tier
---
# List Deployment Tiers

The `tier list` command lists all deployment tiers with their version and showing if _Core Services_ are enabled.

## Options

<dl>

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

<dt><code>--no-beep</code></dt>
<dd>

(optional) Don't emit beep when finished
</dd>

</dl>

## Examples

### List all deployed modules

__Command Line:__
```bash
lash tier list --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.8.0.4) - List all available deployment tiers

Found 8 deployment tiers

TIER             VERSION    STATUS             CORE-SERVICES
Legacy           0.7.0      UPDATE_COMPLETE    ENABLED
ProdOps          0.7.0.8    UPDATE_COMPLETE    ENABLED
Sandbox          0.8.0.4    UPDATE_COMPLETE    ENABLED

Done (finished: 6/25/2020 1:09:28 PM; duration: 00:00:01.7056558)
```
