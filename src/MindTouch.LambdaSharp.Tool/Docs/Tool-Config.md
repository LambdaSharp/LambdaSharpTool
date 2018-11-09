![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Config Command

The `config` command is used to configure λ# CLI. The configuration step optionally creates needed resources for deploying λ# modules and captures deployment preferences. The λ# CLI configuration options are stored in [AWS System Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-paramstore.html), so they can be shared across teams on the same AWS account.

## Options

<dl>

<dt><code>--module-s3-bucket-name &lt;NAME&gt;</code></dt>
<dd>(optional) Existing S3 bucket name for module deployments (blank value creates new bucket)</dd>

<dt><code>--cloudformation-notifications-topic &lt;ARN&gt;</code></dt>
<dd>(optional) Existing SNS topic ARN for CloudFormation notifications (blank value creates new bucket)</dd>

<dt><code>--protect</code></dt>
<dd>(optional) Enable termination protection for the CloudFormation stack</dd>

<dt><code>--force-update</code></dt>
<dd>(optional) Force CLI profile update</dd>

<dt><code>--cli-profile|-CLI &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp CLI profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### Configure a new CLI profile with interactive prompts

__Using Powershell/Bash:__
```bash
dotnet lash config
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Configure LambdaSharp CLI
Configuring a new profile for LambdaSharp CLI
CLI profile name: [Default]
Existing S3 bucket name for module deployments (blank value creates new bucket):
Existing SNS topic ARN for CloudFormation notifications (empty value creates new bucket):
=> Stack creation initiated for LambdaSharpTool-Default
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              LambdaSharpTool-Default (User Initiated)
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     VersionSsmParameter
CREATE_IN_PROGRESS                  AWS::S3::Bucket                                         DeploymentBucket
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         DeploymentNotificationTopic
...
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter (Resource creation Initiated)
CREATE_COMPLETE                     AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              LambdaSharpTool-Default
=> Stack creation finished (finished: 2018-10-25 21:23:05)

Done (duration: 00:00:40.0739292)
```

### Configure a new CLI profile without any prompts

__Using Powershell/Bash:__
```bash
dotnet lash config --cli-profile Team --module-s3-bucket-name="" --cloudformation-notifications-topic=""
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Configure LambdaSharp CLI
Configuring a new profile for LambdaSharp CLI
Creating CLI profile: Team
Creating new S3 bucket
Creating new SNS topic for CloudFormation notifications
=> Stack creation initiated for LambdaSharpTool-Team
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              LambdaSharpTool-Team (User Initiated)
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         DeploymentNotificationTopic
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     StackNameSsmParameter
...
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter (Resource creation Initiated)
CREATE_COMPLETE                     AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              LambdaSharpTool-Team
=> Stack creation finished (finished: 2018-10-25 22:05:20)

Done (duration: 00:00:36.3661853)
```
