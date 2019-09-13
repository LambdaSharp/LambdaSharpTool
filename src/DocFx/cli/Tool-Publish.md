---
title: LambdaSharp CLI - Publish Command
description: Publish a LambdaSharp module to a deployment tier
keywords: cli, build, publish, deployment, tier, module
---
# Publish Module

The `publish` command is used to upload the compiled module and its artifacts to the deployment bucket.

## Arguments

The `publish` command takes an optional path. The path can either refer to a manifest file, a module definition, or a folder containing a `Module.yml` file.

If the path does not refer to a manifest file, the `publish` command invokes the `build` command to compile the module and its artifacts.

```bash
lash new function MyNewFunction
```

## Options

<dl>

<dt><code>--no-assembly-validation</code></dt>
<dd>

(optional) Disable validating LambdaSharp assembly references in function project files
</dd>

<dt><code>-c|--configuration &lt;CONFIGURATION&gt;</code></dt>
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

### Build and publish module in current folder

__Using PowerShell/Bash:__
```bash
lash publish
```

Output:
```
LambdaSharp CLI (v0.7.0) - Publish LambdaSharp module

Reading module: Module.yml
Compiling: Demo.SlackTodo (v1.0-DEV)
=> Building function SlackCommand [netcoreapp2.1, Release]
=> Module compilation done: bin\cloudformation.json
Publishing module: Demo.SlackTodo
=> Uploading artifact: s3://lambdasharp-bucket-name/lambdasharp-bucket-name/LambdaSharp/Demo.SlackTodo/.artifacts/function_Demo.SlackTodo_SlackCommand_E0F4477DDAFDC152C8B66343657E9425.zip
=> Uploading template: s3://lambdasharp-bucket-name/lambdasharp-bucket-name/LambdaSharp/Demo.SlackTodo/.artifacts/cloudformation_Demo.SlackTodo_939992254E194760372083264D08D795.json

Done (finished: 9/5/2019 1:07:28 PM; duration: 00:00:11.1692368)
```

### Publish manifest

__Using PowerShell/Bash:__
```bash
lash publish bin/cloudformation.json
```

Output:
```
LambdaSharp CLI (v0.7.0) - Publish LambdaSharp module
Publishing module: Demo.SlackTodo
=> Uploading artifact: s3://lambdasharp-bucket-name/lambdasharp-bucket-name/LambdaSharp/Demo.SlackTodo/.artifacts/function_Demo.SlackTodo_SlackCommand_E0F4477DDAFDC152C8B66343657E9425.zip
=> Uploading template: s3://lambdasharp-bucket-name/lambdasharp-bucket-name/LambdaSharp/Demo.SlackTodo/.artifacts/cloudformation_Demo.SlackTodo_939992254E194760372083264D08D795.json

Done (finished: 9/5/2019 1:07:28 PM; duration: 00:00:11.1692368)
```
