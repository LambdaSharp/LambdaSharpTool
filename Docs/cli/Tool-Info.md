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

### Show information for Default profile and Sandbox tier

__Command Line:__
```bash
lash info --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.7.0.11) - Show LambdaSharp information
LambdaSharp Deployment Tier
    Name: Sandbox
    Version: 0.7.0.11
    Core Services: Enabled
    Deployment S3 Bucket: lambdasharp-bucket-name
    API Gateway Role: arn:aws:iam::************:role/LambdaSharp-ApiGatewayRole
Git
    Branch: master
    SHA: DIRTY-887d4fa82845aa09118aba5fda7b2e884f8fe28e
AWS
    Region: us-east-1
    Account Id: ************
    Lambda Storage: 0.54GB of 75GB (0.71%)
    Lambda Reserved Executions: 14 of 1,000 (1.40%)
Tools
    .NET Core CLI Version: 3.1.100
    Git CLI Version: 2.18.0.windows.1
    Amazon.Lambda.Tools: 4.0.0

Done (finished: 3/31/2020 4:38:00 PM; duration: 00:00:02.7928772)
```
