---
title: LambdaSharp CLI Util Command - List all Lambda functions by CloudFormation stack
description: List Lambda functions by CloudFormation stack with their runtime and last used date
keywords: cli, lambda, cloudformation, logs
---
# List all Lambda functions by CloudFormation stack

The `util list-lambdas` command is used to list all Lambda functions grouped by their respective CloudFormation stack or shown as orphans if deployed independently. Each Lambda function is listed with its runtime and last event timestamps from the CloudWatch logs to see how recently it was used. Similarly, CloudFormation stacks deployed by _LambdaSharp.Tool_ show their module information and the tool version they were compiled with.

## Options

<dl>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS profile from the AWS credentials file
</dd>

<dt><code>--aws-region &lt;NAME&gt;</code></dt>
<dd>

(optional) Use a specific AWS region (default: read from AWS profile)
</dd>

<dt><code>--no-ansi</code></dt>
<dd>

(optional) Disable colored ANSI terminal output
</dd>

</dl>

## Examples

### List all Lambda functions by CloudFormation stack

__Using PowerShell/Bash:__
```bash
lash util list-lambdas
```

Output:
```
LambdaSharp CLI (v0.7.0.16) - List all Lambda functions by CloudFormation stack

Analyzing 4 CloudFormation stacks and 8 Lambda functions

SteveBv7-Demo-TwitterNotifier (Demo.TwitterNotifier:1.0-DEV) [lash 0.7.0.15]:
    NotifyFunction                       dotnetcore3.1    2020-04-26

SteveBv7-Demo-TwitterNotifier-TwitterNotify-1F8924TBHKNJ9 (LambdaSharp.Twitter.Query:0.7.0.15) [lash 0.7.0.15]:
    QueryFunction                        dotnetcore2.1    2020-04-28

SteveBv7-LambdaSharp-Core (LambdaSharp.Core:0.7.0.15) [lash 0.7.0.15]:
    ProcessLogEventsFunction             dotnetcore2.1    2020-04-28
    RegistrationFunction                 dotnetcore2.1    2020-04-24

SteveBv7-Sample-Metric (Sample.Metric:1.0-DEV) [lash 0.7.0.15]:
    MyFunction                           dotnetcore3.1    2020-04-20

ORPHANS:
    github-auth                          nodejs8.10       2020-02-28
    qa-upload-data-test                  dotnetcore2.1    2020-02-27(*)
    s3Lambda                             dotnetcore2.1    2019-01-14

(*) Showing Lambda last-modified date, because last event timestamp in CloudWatch log stream is not available

Done (finished: 4/28/2020 12:24:22 PM; duration: 00:00:04.0566986)
```
