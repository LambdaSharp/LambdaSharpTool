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
<dd>(optional) Provide a path to a local check-out of the LambdaSharp runtime modules (default: LAMBDASHARP environment variable)</dd>

<dt><code>--use-published</code></dt>
<dd>(optional) Force the init command to use the published LambdaSharp runtime modules</dd>

<dt><code>--cli-profile|-CLI &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp CLI profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### Create a new deployment tier or update an existing one

__Using Powershell/Bash:__
```bash
dotnet lash init --tier Sandbox
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Initialize LambdaSharp deployment tier
Creating new deployment tier 'Sandbox'
Deploying stack: Sandbox-LambdaSharp [LambdaSharp]
=> Stack creation initiated for Sandbox-LambdaSharp
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-LambdaSharp (User Initiated)
CREATE_IN_PROGRESS                  AWS::SQS::Queue                                         DeadLetterQueue
CREATE_IN_PROGRESS                  AWS::SQS::Queue                                         DeadLetterQueue (Resource creation Initiated)
CREATE_IN_PROGRESS                  AWS::KMS::Key                                           DefaultSecretKey
...
CREATE_IN_PROGRESS                  AWS::Logs::SubscriptionFilter                           ResourceHandlerLogGroupSubscription (Resource creation Initiated)
CREATE_COMPLETE                     AWS::Logs::SubscriptionFilter                           ResourceHandlerLogGroupSubscription
CREATE_COMPLETE                     AWS::Lambda::Permission                                 ResourceHandlerCustomResourceTopicSnsPermission
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              Sandbox-LambdaSharpS3PackageLoader
=> Stack creation finished (finished: 2018-10-26 12:55:34)
Stack output values:
=> Custom resource for for deploying packages to S3 buckets: arn:aws:sns:us-west-2:254924790709:Sandbox-LambdaSharpS3PackageLoader-CustomResourceTopic-1LKRGDWA8M5XV
=> ModuleName: LambdaSharpS3PackageLoader
=> ModuleVersion: 0.4

Done (duration: 00:05:41.6557981)
```

## For λ# Contributors
The `init` command builds and deploys the local λ# runtime modules when the `LAMBDASHARP` environment variable is set. To force `init` to use the published λ# runtime modules instead, append the `--use-published` option. Alternatively, the `--local` option can be used to provide the location of a local check-out of the LambdaSharpTool source tree.

