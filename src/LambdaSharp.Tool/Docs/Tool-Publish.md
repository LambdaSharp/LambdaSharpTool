![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Publish Command

The `publish` command is used to upload the compiled module and its assets to the deployment bucket.

## Arguments

The `publish` command takes an optional path. The path can either refer to a manifest file, a module definition, or a folder containing a `Module.yml` file.

If the path does not refer to a manifest file, the `publish` command invokes the `build` command to compile the module and its assets.

```bash
lash new function MyNewFunction
```

## Options

<dl>

<dt><code>--no-assembly-validation</code></dt>
<dd>(optional) Disable validating LambdaSharp assembly references in function project files</dd>

<dt><code>-c|--configuration &lt;CONFIGURATION&gt;</code></dt>
<dd>(optional) Build configuration for function projects (default: "Release")</dd>

<dt><code>--gitsha &lt;VALUE&gt;</code></dt>
<dd>(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)</dd>

<dt><code> -o|--output &lt;DIRECTORY&gt;</code></dt>
<dd>(optional) Path to output directory (default: bin)</dd>

<dt><code>-selector &lt;NAME&gt;</code></dt>
<dd>(optional) Selector for resolving conditional compilation choices in module</dd>

<dt><code>--cf-output &lt;FILE&gt;</code></dt>
<dd>(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)</dd>

<dt><code>--dryrun[:&lt;LEVEL&gt;]</code></dt>
<dd>(optional) Generate output assets without deploying (0=everything, 1=cloudformation)</dd>

<dt><code>--cli-profile|-C &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp CLI profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### Build and publish module in current folder

__Using PowerShell/Bash:__
```bash
lash publish
```

Output:
```
LambdaSharp CLI (v0.5) - Publish LambdaSharp module

Compiling module: Module.yml
=> Building function RecordMessage [netcoreapp2.1, Release]
=> Building function SlackCommand [netcoreapp2.1, Release]
=> Module compilation done: C:\LambdaSharpTool\Demos\Demo\bin\cloudformation.json
Publishing module: LambdaSharp.Demo
=> Uploading asset: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/function_RecordMessage_8A0A4D0DA5B090BD33D779EF16FE7470.zip
=> Uploading asset: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/function_SlackCommand_30C238770176A7AE6955A519FC6A283A.zip
=> Uploading template: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/cloudformation_v1.0-DEV_A556D13161D90F32959CDE5EC121B7D0.json

Done (finished: 1/18/2019 1:26:33 PM; duration: 00:00:12.9332067)
```

### Publish manifest

__Using PowerShell/Bash:__
```bash
lash publish Demo/bin/cloudformation.json
```

Output:
```
LambdaSharp CLI (v0.5) - Publish LambdaSharp module
Publishing module: LambdaSharp.Demo
=> Uploading asset: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/function_RecordMessage_8A0A4D0DA5B090BD33D779EF16FE7470.zip
=> Uploading asset: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/function_SlackCommand_30C238770176A7AE6955A519FC6A283A.zip
=> Uploading template: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/cloudformation_v1.0-DEV_A556D13161D90F32959CDE5EC121B7D0.json

Done (finished: 1/18/2019 1:28:06 PM; duration: 00:00:02.9318400)
```
