![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Init Command

The `init` command is used to both initialize a new deployment tier and update an existing one.

## Options

<dl>

<dt><code>--allow-data-loss</code></dt>
<dd>(optional) Allow CloudFormation resource update operations that could lead to data loss</dd>

<dt><code>--protect</code></dt>
<dd>(optional) Enable termination protection for the CloudFormation stack</dd>

<dt><code>--force-deploy</code></dt>
<dd>(optional) Force module deployment</dd>

<dt><code>--version &lt;VERSION&gt;</code></dt>
<dd>(optional) Specify version for LambdaSharp modules (default: same as CLI version)</dd>

<dt><code>--local &lt;PATH&gt;</code></dt>
<dd>(optional) Provide a path to a local check-out of the LambdaSharp modules (default: LAMBDASHARP environment variable)</dd>

<dt><code>--use-published</code></dt>
<dd>(optional) Force the init command to use the published LambdaSharp modules</dd>

<dt><code>--parameters &lt;FILE&gt;</code></dt>
<dd>(optional) Specify filename to read module parameters from (default: none)</dd>

<dt><code>--force-publish</code></dt>
<dd>(optional) Publish modules and their assets even when no changes were detected</dd>

<dt><code>--prompt-all</code></dt>
<dd>(optional) Prompt for all missing parameters values (default: only prompt for missing parameters with no default value)</dd>

<dt><code>--prompts-as-errors</code></dt>
<dd>(optional) Missing parameters cause an error instead of a prompts (use for CI/CD to avoid unattended prompts)</dd>

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

### Create a new deployment tier or update an existing one

__Using PowerShell/Bash:__
```bash
lash init --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.5) - Initialize LambdaSharp deployment tier
Creating new deployment tier 'Sandbox'
Resolving module reference: LambdaSharp.Core:0.5
=> Validating module for deployment tier

Deploying stack: Sandbox--LambdaSharp-Core [LambdaSharp.Core:0.5]
=> Stack create initiated for Sandbox--LambdaSharp-Core [CAPABILITY_IAM]
REVIEW_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox--LambdaSharp-Core (User Initiated)
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox--LambdaSharp-Core (User Initiated)
CREATE_IN_PROGRESS                  AWS::SQS::Queue                                         DeadLetterQueue::Resource
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         UsageReportTopic
...
CREATE_COMPLETE                     AWS::Lambda::Permission                                 Registration::Source1Permission
CREATE_IN_PROGRESS                  AWS::KMS::Alias                                         DefaultSecretKeyAlias (Resource creation Initiated)
CREATE_COMPLETE                     AWS::KMS::Alias                                         DefaultSecretKeyAlias
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Core
=> Stack create finished
Stack output values:
=> Dead Letter Queue (ARN): arn:aws:sqs:us-east-1:123456789012:Sandbox-LambdaSharp-Core-DeadLetterQueueResource-1RU4L5WQ0VWEZ
=> Default Secret Key (ARN): arn:aws:kms:us-east-1:123456789012:key/42f85bb4-c254-43b1-90de-afa986bb906c
=> Resource type for LambdaSharp function registrations: arn:aws:sns:us-east-1:123456789012:Sandbox-LambdaSharp-Core-RegistrationTopic-OYSNGOC85DP7
=> Resource type for LambdaSharp module registrations: arn:aws:sns:us-east-1:123456789012:Sandbox-LambdaSharp-Core-RegistrationTopic-OYSNGOC85DP7
=> Logging Stream (ARN): arn:aws:kinesis:us-east-1:123456789012:stream/Sandbox-LambdaSharp-Core-LoggingStreamResource-131NRS53BQZBN
=> Role for writing CloudWatch logs to the Kinesis stream: arn:aws:iam::123456789012:role/Sandbox-LambdaSharp-Core-LoggingStreamRole-159RDENYID067
=> Module: LambdaSharp.Core:0.5

Done (finished: 1/18/2019 6:54:05 AM; duration: 00:02:22.8247619)
```

## For λ# Contributors
The `init` command builds and deploys the local λ# Core module when the `LAMBDASHARP` environment variable is set. To force `init` to use the published λ# Core module instead, append the `--use-published` option. Alternatively, the `--local` option can be used to provide the location of a local check-out of the LambdaSharpTool source tree.

