![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - List Command

The `list` command is used to list all deployed modules on a deployment tier.

## Options

<dl>

<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>(optional) Name of deployment tier (default: <code>LAMBDASHARP_TIER</code> environment variable)</dd>

<dt><code>--cli-profile|-CLI &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp CLI profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### List all deployed modules

__Using Powershell/Bash:__
```bash
dotnet lash list --tier Sandbox
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - List deployed LambdaSharp modules

MODULE                        STATUS                DATE
LambdaSharp                   [UPDATE_COMPLETE]     2018-10-25 13:57:12
LambdaSharpRegistrar          [UPDATE_COMPLETE]     2018-10-25 13:58:56
LambdaSharpS3Subscriber       [UPDATE_COMPLETE]     2018-10-25 13:59:47
LambdaSharpS3PackageLoader    [UPDATE_COMPLETE]     2018-10-25 14:00:20

Found 4 modules for deployment tier 'Sandbox'

Done (duration: 00:00:01.7553089)
```
