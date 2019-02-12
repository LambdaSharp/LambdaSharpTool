![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Config Command

The `config` command is used to configure the λ# CLI. The configuration step optionally creates resources for deploying λ# modules and captures deployment preferences. The λ# CLI configuration options are stored in [AWS System Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-paramstore.html), so they can be shared across teams on the same AWS account.

## Options

<dl>

<dt><code>--existing-s3-bucket-name &lt;NAME&gt;</code></dt>
<dd>(optional) Existing S3 bucket name for module deployments (blank value creates new bucket)</dd>

<dt><code>--requested-s3-bucket-name &lt;NAME&gt;</code></dt>
<dd>(optional) Requested S3 bucket name for module deployments (blank value assigns automatic name)</dd>

<dt><code>--cloudformation-notifications-topic &lt;ARN&gt;</code></dt>
<dd>(optional) Existing SNS topic ARN for CloudFormation notifications (blank value creates new topic)</dd>

<dt><code>--protect</code></dt>
<dd>(optional) Enable termination protection for the CloudFormation stack</dd>

<dt><code>--force-update</code></dt>
<dd>(optional) Force CLI profile update</dd>

<dt><code>--cli-profile|-C &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp CLI profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### Configure a new CLI profile with interactive prompts

__Using PowerShell/Bash:__
```bash
lash config
```

Output:
```
LambdaSharp CLI (v0.5) - Configure LambdaSharp CLI
Configuring a new profile for LambdaSharp CLI
|=> CLI profile name: [Default]

Configuring LambdaSharp Tool (v0.5) Parameters
|=> Name of an existing S3 bucket for LambdaSharp deployments (if blank, a new S3 bucket is created):
|=> (optional) Name of newly created S3 bucket (if blank, a unique name is generated):
|=> ARN of existing SNS topic for CloudFormation notifications (if blank, an SNS topic is created):

=> Stack creation initiated for LambdaSharpTool-Default
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              LambdaSharpTool-Default (User Initiated)
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         DeploymentNotificationTopicResource
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     VersionSsmParameter
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     StackNameSsmParameter
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         DeploymentNotificationTopicResource (Resource creation Initiated)
...
CREATE_COMPLETE                     AWS::S3::BucketPolicy                                   DeploymentBucketPolicy
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter (Resource creation Initiated)
CREATE_COMPLETE                     AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              LambdaSharpTool-Default
=> Stack creation finished

Done (finished: 1/18/2019 9:55:27 AM; duration: 00:00:42.6786101)
```

### Configure a new CLI profile without prompts

__Using Bash:__
```bash
lash config \
    --cli-profile Team \
    --existing-s3-bucket-name "" \
    --requested-s3-bucket-name "" \
    --cloudformation-notifications-topic ""
```
__Using PowerShell:__
```powershell
lash config ^
    --cli-profile Team ^
    --existing-s3-bucket-name "" ^
    --requested-s3-bucket-name "" ^
    --cloudformation-notifications-topic ""
```

Output:
```
LambdaSharp CLI (v0.5) - Configure LambdaSharp CLI
Configuring a new profile for LambdaSharp CLI
Creating CLI profile: Team
=> Stack creation initiated for LambdaSharpTool-Team
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              LambdaSharpTool-Team (User Initiated)
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     VersionSsmParameter
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         DeploymentNotificationTopicResource
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     StackNameSsmParameter
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         DeploymentNotificationTopicResource (Resource creation Initiated)
...
CREATE_COMPLETE                     AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentNotificationTopicSsmParameter (Resource creation Initiated)
CREATE_COMPLETE                     AWS::SSM::Parameter                                     DeploymentNotificationTopicSsmParameter
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              LambdaSharpTool-Team
=> Stack creation finished

Done (finished: 1/18/2019 10:06:41 AM; duration: 00:00:48.0435689)
```
