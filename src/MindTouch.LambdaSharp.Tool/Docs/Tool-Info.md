![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Info Command

The `info` command is used to show information about the current CLI profile, λ# environment, and installed command line tools.

## Options

<dl>

<dt><code>--show-sensitive</code></dt>
<dd>(optional) Show sensitive information</dd>

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

### Show information for Default profile and Sandbox tier

__Using Powershell/Bash:__
```bash
dotnet lash info --tier Sandbox
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Show LambdaSharp information
LambdaSharp CLI
    Profile: Default
    Version: 0.4
    Module Deployment S3 Bucket: lambdasharp-bucket-name
    Module Deployment Notifications Topic: arn:aws:sns:us-west-2:************:LambdaSharpTool-Sandbox-DeploymentNotificationTopic-1V8UD7UQVW3KD
LambdaSharp Deployment Tier
    Name: Sandbox
    Runtime Version: 0.4
Git SHA: dd84a2a4b87dcf2e4a802b79d12c489c30836623
AWS
    Region: us-west-2
    Account Id: ************
Tools
    .NET Core CLI Version: 2.1.402
    Git CLI Version: 2.18.0.windows.1

Done (duration: 00:00:01.3788826)
```
