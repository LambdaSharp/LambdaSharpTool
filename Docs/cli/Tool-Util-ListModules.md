---
title: LambdaSharp CLI Util Command - List published modules at an origin
description: List published LambdaSharp modules at an origin
keywords: cli, lambda, modules
---
# List published LambdaSharp modules at an origin

The `util list-modules` command is used to list all published LambdaSharp modules with their version numbers. By default, pre-release versions are not included unless the `--include-prerelease` command line option is specified.

## Arguments

The `util list-modules` command takes a single argument. The argument can either be the name of an S3 bucket or a module reference. If the argument is a module reference, only versions for the specified module are shown.

```bash
lash list-modules lambdasharp
```
-OR-
```bash
lash list-modules LambdaSharp.S3.IO@lambdasharp
```

## Options

<dl>

<dt><code>--bucket &lt;BUCKETNAME&gt;</code></dt>
<dd>

List modules from this S3 bucket (default: match argument)
</dd>

<dt><code>--origin &lt;ORIGIN&gt;</code></dt>
<dd>

(optional) List modules from this origin (default: match argument)
</dd>

<dt><code>--include-prerelease</code></dt>
<dd>

(optional) Show pre-releases versions (default: omit pre-release versions)
</dd>

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

<dt><code>--quiet</code></dt>
<dd>

(optional) Don't show banner or execution time
</dd>

</dl>

## Examples

### List all modules from the lambdasharp origin

__Using PowerShell/Bash:__
```bash
lash util list-modules lambdasharp
```

Output:
```
LambdaSharp CLI (v0.8.0.9) - List all modules at origin

LambdaRobots.HotShotRobot: 1.0, 1.2
LambdaRobots.Server: 1.0, 1.2
LambdaRobots.TargetRobot: 1.0, 1.2
LambdaRobots.YosemiteSamRobot: 1.0, 1.2
LambdaSharp.App.Api: 0.8.1.0
LambdaSharp.App.Bucket: 0.8.1.0
LambdaSharp.Core: 0.7.0, 0.7.0.8, 0.8.0.0, 0.8.0.2, 0.8.0.3, 0.8.0.4, 0.8.0.6, 0.8.1.0
LambdaSharp.S3.IO: 0.7.0, 0.7.0.3, 0.7.0.8, 0.8.0.0, 0.8.0.2, 0.8.0.3, 0.8.0.4, 0.8.0.6, 0.8.1.0
LambdaSharp.S3.Subscriber: 0.7.0, 0.7.0.3, 0.7.0.8, 0.8.0.0, 0.8.0.2, 0.8.0.3, 0.8.0.4, 0.8.0.6, 0.8.1.0
LambdaSharp.Twitter.Query: 0.7.0, 0.7.0.3, 0.7.0.8, 0.8.0.0, 0.8.0.2, 0.8.0.3, 0.8.0.4, 0.8.0.6, 0.8.1.0

Done (finished: 8/31/2020 1:17:52 PM; duration: 00:00:01.7544419)
```

### List all version of the LambdaSharp.S3.IO@lambdasharp module

__Using PowerShell/Bash:__
```bash
lash util list-modules LambdaSharp.S3.IO@lambdasharp
```

Output:
```
LambdaSharp CLI (v0.8.0.9) - List all modules at origin

LambdaSharp.S3.IO: 0.7.0, 0.7.0.3, 0.7.0.8, 0.8.0.0, 0.8.0.2, 0.8.0.3, 0.8.0.4, 0.8.0.6, 0.8.1.0

Done (finished: 8/31/2020 1:18:47 PM; duration: 00:00:01.7517330)
```

### List all version of the LambdaSharp.S3.IO@lambdasharp module, including pre-release versions

__Using PowerShell/Bash:__
```bash
lash util list-modules --include-prerelease LambdaSharp.S3.IO@lambdasharp
```

Output:
```
LambdaSharp CLI (v0.8.0.9) - List all modules at origin

LambdaSharp.S3.IO: 0.7.0-rc4, 0.7.0-rc5, 0.7.0-rc6, 0.7.0-rc7, 0.7.0, 0.7.0.3, 0.7.0.8, 0.7.1.0-rc1, 0.8.0.0, 0.8.0.2, 0.8.0.3, 0.8.0.4, 0.8.0.6, 0.8.1.0

Done (finished: 8/31/2020 1:19:26 PM; duration: 00:00:01.7473406)
```
