![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - List Command

The `list` command is used to list all deployed modules on a deployment tier.

## Options

<dl>

<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>(optional) Name of deployment tier (default: <code>LAMBDASHARP_TIER</code> environment variable)</dd>

<dt><code>--cli-profile|-C &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp CLI profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### List all deployed modules

__Using PowerShell/Bash:__
```bash
lash list --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.5) - List deployed LambdaSharp modules

MODULE               STATUS             DATE
LambdaSharp-Core     [CREATE_COMPLETE]  2019-01-18 11:17:25
LambdaSharp-S3-IO    [CREATE_COMPLETE]  2019-01-18 11:28:25

Found 2 modules for deployment tier 'Sandbox'

Done (finished: 1/18/2019 11:29:44 AM; duration: 00:00:00.9423924)
```
