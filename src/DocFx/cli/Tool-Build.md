---
title: LambdaSharp CLI - Build Command
description: Build a LambdaSharp module and generate its artifacts
keywords: cli, build, deployment tier, module, artifact
---
# Build Module

The `build` command compiles the module in preparation for publishing. If the module contains functions, the dependencies are resolved, the function project is built, and a Lambda-ready package is created. If the module contains file packages, the files are compressed into a zip archive.

## Arguments

The `build` command takes an optional path. The path can either refer to a module definition or a folder containing a `Module.yml` file.

```bash
lash build
```

## Options

<dl>

<dt><code>--no-assembly-validation</code></dt>
<dd>

(optional) Disable validating LambdaSharp assemblies
</dd>

<dt><code>--no-dependency-validation</code></dt>
<dd>

(optional) Disable validating LambdaSharp module dependencies
</dd>

<dt><code>--configuration|-c &lt;CONFIGURATION&gt;</code></dt>
<dd>

(optional) Build configuration for function projects (default: "Release")
</dd>

<dt><code>--git-sha &lt;VALUE&gt;</code></dt>
<dd>

(optional) Git SHA of most recent git commit (default: invoke `git rev-parse HEAD` command)
</dd>

<dt><code>--git-branch &lt;VALUE&gt;</code></dt>
<dd>

(optional) (optional) Git branch name (default: invoke `git rev-parse --abbrev-ref HEAD` command)
</dd>

<dt><code>--output|-o  &lt;DIRECTORY&gt;</code></dt>
<dd>

(optional) Path to output directory (default: bin)
</dd>

<dt><code>--selector &lt;NAME&gt;</code></dt>
<dd>

(optional) Selector for resolving conditional compilation choices in module
</dd>

<dt><code>--cfn-output &lt;FILE&gt;</code></dt>
<dd>

(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)
</dd>

<dt><code>--module-version &lt;VERSION&gt;</code></dt>
<dd>

(optional) Override the module version
</dd>

<dt><code>--dryrun[:&lt;LEVEL&gt;]</code></dt>
<dd>

(optional) Generate output artifacts without deploying (0=everything, 1=cloudformation)
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

### Build module in current folder

__Using PowerShell/Bash:__
```bash
lash build
```

Output:
```
LambdaSharp CLI (v0.5) - Build LambdaSharp module

Reading module: Module.yml
Compiling: Demo.SlackTodo (v1.0-DEV)
=> Building function RecordMessage [netcoreapp2.1, Release]
=> Building function SlackCommand [netcoreapp2.1, Release]
=> Module compilation done: C:\LambdaSharpTool\Demos\Demo\bin\cloudformation.json

Done (finished: 1/17/2019 3:57:27 PM; duration: 00:00:21.2642565)
```

### Build module in a sub-folder

__Using PowerShell/Bash:__
```bash
lash build Demo
```

Output:
```
LambdaSharp CLI (v0.5) - Build LambdaSharp module

Reading module: Module.yml
Compiling: Demo.SlackTodo (v1.0-DEV)
=> Building function RecordMessage [netcoreapp2.1, Release]
=> Building function SlackCommand [netcoreapp2.1, Release]
=> Module compilation done: C:\LambdaSharpTool\Demos\Demo\bin\cloudformation.json

Done (finished: 1/17/2019 3:57:27 PM; duration: 00:00:21.2642565)
```
