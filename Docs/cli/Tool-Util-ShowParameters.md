---
title: LambdaSharp CLI Util Command - Show Processed YAML Parameters File
description: Process a YAML parameters file and show all computed key-value pairs
keywords: cli, lambda, cloudformation, config
---
# Show processed YAML parameters file

The `util show-parameters` command is used to process a YAML parameters file and show the produced outcome. This command is useful in testing the correctness of the parameters file.

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

### Process a YAML parameters file and show the outcome

__Command Line:__
```bash
lash util show-lambdas myVpcParameters.yml
```

Output:
```
LambdaSharp CLI (v0.8.0.4) - Show Processed YAML Parameters File

VpcId: vpc-12345678
VpcLambdaSubnets: subnet-12345678,subnet-23456789
VpcSecurityGroup: sg-12345678

Done (finished: 6/29/2020 9:59:27 PM; duration: 00:00:01.0395664)
```
