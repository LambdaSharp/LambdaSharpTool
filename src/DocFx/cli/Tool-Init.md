# Initialize Deployment Tier

The `init` command is used to both create a new deployment tier and update an existing one. The resources required for a new deployment tier will be created unless provided.

The `--quick-start` option minimizes the setup time by disabling the core services and assuming safe defaults for all prompts. This option is useful for learning about λ# and getting started quickly. However, **DO NOT** use this option in production or test environments!

## Options

<dl>

<dt><code>--allow-data-loss</code></dt>
<dd>

(optional) Allow CloudFormation resource update operations that could lead to data loss
</dd>

<dt><code>--protect</code></dt>
<dd>

(optional) Enable termination protection for the CloudFormation stack
</dd>

<dt><code>--xray[:&lt;LEVEL&gt;]</code></dt>
<dd>

(optional) Enable service-call tracing with AWS X-Ray for all resources in module  (0=Disabled, 1=RootModule, 2=AllModules; RootModule if LEVEL is omitted)
</dd>

<dt><code>--version &lt;VERSION&gt;</code></dt>
<dd>

(optional) Specify version for LambdaSharp modules (default: same as CLI version)
</dd>

<dt><code>--parameters &lt;FILE&gt;</code></dt>
<dd>

(optional) Specify filename to read module parameters from (default: none)
</dd>

<dt><code>--force-publish</code></dt>
<dd>

(optional) Publish modules and their assets even when no changes were detected
</dd>

<dt><code>--force-deploy</code></dt>
<dd>

(optional) Force module deployment
</dd>

<dt><code>--quick-start</code></dt>
<dd>

(optional) Use safe defaults for quickly setting up a LambdaSharp deployment tier.
</dd>

<dt><code>--core-services &lt;VALUE&gt;</code></dt>
<dd>

(optional) Select if LambdaSharp.Core services should be enabled or not (either Enabled or Disabled, default prompts)
</dd>

<dt><code>--existing-s3-bucket-name &lt;NAME&gt;</code></dt>
<dd>

(optional) Existing S3 bucket name for module deployments (blank value creates new bucket)
</dd>

<dt><code>--local &lt;PATH&gt;</code></dt>
<dd>

(optional) Provide a path to a local check-out of the LambdaSharp modules (default: LAMBDASHARP environment variable)
</dd>

<dt><code>--use-published</code></dt>
<dd>

(optional) Force the init command to use the published LambdaSharp modules
</dd>

<dt><code>--prompt-all</code></dt>
<dd>

(optional) Prompt for all missing parameters values (default: only prompt for missing parameters with no default value)
</dd>

<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>

(optional) Name of deployment tier (default: <code>LAMBDASHARP_TIER</code> environment variable)
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

<dt><code>--prompts-as-errors</code></dt>
<dd>

(optional) Missing parameters cause an error instead of a prompts (use for CI/CD to avoid unattended prompts)
</dd>

</dl>

## Examples

### Creating a new deployment tier using the `--quick-start` option

__Using PowerShell/Bash:__
```bash
lash init --tier Sandbox --quick-start
```
Output:
```
LambdaSharp CLI (v0.7.0) - Create or update a LambdaSharp deployment tier
Creating LambdaSharp tier
=> Stack creation initiated for Sandbox-LambdaSharp-Core
CREATE_COMPLETE    AWS::CloudFormation::Stack    Sandbox-LambdaSharp-Core
CREATE_COMPLETE    AWS::S3::Bucket               DeploymentBucketResource
=> Stack creation finished
=> Checking API Gateway role

Done (finished: 7/15/2019 10:20:09 AM; duration: 00:01:14.0338861)
```

## For λ# Contributors
The `init` command builds and deploys the local LambdaSharp.Core module when the `LAMBDASHARP` environment variable is set. To force `init` to use the published λ# Core module instead, append the `--use-published` option. Alternatively, the `--local` option can be used to provide the location of a local check-out of the LambdaSharpTool source tree.

