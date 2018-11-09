![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Build Command

The `build` command compiles the module and all of its assets in preparation for publishing. If the module contains functions, their dependencies are resolved, the function project is built, and a Lambda-ready package is created. If the module contains file packages, the files are compressed into a zip archive.

## Arguments

The `build` command takes an optional path. The path can either refer to a module definition or a folder containing a `Module.yml` file.

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

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### Build module in current folder

__Using Powershell/Bash:__
```bash
dotnet lash build
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Build LambdaSharp module

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

Done (duration: 00:00:11.0292989)
```

### Build module in a nested folder

__Using Powershell/Bash:__
```bash
dotnet lash build Demo
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Build LambdaSharp module

Compiling module: Demo\Module.yml
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

Done (duration: 00:00:11.0292989)
```
