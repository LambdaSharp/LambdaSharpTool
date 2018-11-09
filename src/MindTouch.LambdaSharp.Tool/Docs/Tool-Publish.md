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

<dt><code>--skip-assembly-validation</code></dt>
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

<dt><code>--cli-profile|-CLI &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp CLI profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### Build and publish module in current folder

__Using Powershell/Bash:__
```bash
dotnet lash publish
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Publish LambdaSharp module

Compiling module: Module.yml
Building function RecordMessage [netcoreapp2.1, Release]
=> Restoring project dependencies
=> Building AWS Lambda package
=> Decompressing AWS Lambda package
=> Finalizing AWS Lambda package
Building function SlackCommand [netcoreapp2.1, Release]
=> Restoring project dependencies
=> Building AWS Lambda package
=> Decompressing AWS Lambda package
=> Finalizing AWS Lambda package
=> Module compilation done
Publishing module: Demo
=> Uploading function: s3://lambdasharp-bucket-name/Modules/Demo/Assets/function_RecordMessage_4E05BDFA74DAC87A05165A4D5B609B39.zip
=> Uploading function: s3://lambdasharp-bucket-name/Modules/Demo/Assets/function_SlackCommand_8207022C95970006F597FF6060366C34.zip
=> Uploading template: s3://lambdasharp-bucket-name/Modules/Demo/Assets/cloudformation_v1.0_F2E9DA098FB8B0EB118F5839947467A6.json
=> Uploading manifest: s3://lambdasharp-bucket-name/Modules/Demo/Assets/manifest_v1.0_F2E9DA098FB8B0EB118F5839947467A6.json

Done (duration: 00:00:27.8416454)
```

### Publish manifest

__Using Powershell/Bash:__
```bash
dotnet lash publish Demo/bin/manifest.json
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Publish LambdaSharp module
Publishing module: Demo
=> Uploading function: s3://lambdasharp-bucket-name/Modules/Demo/Assets/function_RecordMessage_4E05BDFA74DAC87A05165A4D5B609B39.zip
=> Uploading function: s3://lambdasharp-bucket-name/Modules/Demo/Assets/function_SlackCommand_8207022C95970006F597FF6060366C34.zip
=> Uploading template: s3://lambdasharp-bucket-name/Modules/Demo/Assets/cloudformation_v1.0_F2E9DA098FB8B0EB118F5839947467A6.json
=> Uploading manifest: s3://lambdasharp-bucket-name/Modules/Demo/Assets/manifest_v1.0_F2E9DA098FB8B0EB118F5839947467A6.json

Done (duration: 00:00:16.6464580)
```
