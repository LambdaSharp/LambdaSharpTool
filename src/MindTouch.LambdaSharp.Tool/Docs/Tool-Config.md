![λ#](../../../Docs/LambdaSharp_v2_small.png)

# λ# Tool - Config Command

The `config` command is used to configure λ# tool. The configuration step optionally creates needed resources for deploying λ# modules and captures deployment preferences. The λ# tool configuration options are stored in [AWS System Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-paramstore.html), so they can be shared across teams on the same AWS account.

__Using Powershell/Bash:__
```bash
dotnet lash config
```

## Options

<dl>

<dt><code>--module-s3-bucket-name &lt;NAME&gt;</code></dt>
<dd>(optional) Existing S3 bucket name for module deployments (blank value creates new bucket)</dd>

<dt><code>--module-s3-bucket-path &lt;PATH&gt;</code></dt>
<dd>(optional) S3 bucket path for module deployments (default: Modules/)</dd>

<dt><code>--cloudformation-notifications-topic &lt;ARN&gt;</code></dt>
<dd>(optional) Existing SNS topic ARN for CloudFormation notifications (blank value creates new bucket)</dd>

<dt><code>--protect</code></dt>
<dd>(optional) Enable termination protection for the CloudFormation stack</dd>

<dt><code>--tool-profile|-TP &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific LambdaSharp tool profile (default: Default)</dd>

<dt><code>--aws-profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>

<dt><code>--verbose|-V:&lt;LEVEL&gt;</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>

</dl>

## Examples

### Configure a new tool profile with interactive prompts

__Using Powershell/Bash:__
```bash
dotnet lash config
```

Output:
```
MindTouch LambdaSharp Tool (v0.4) - Configure LambdaSharp environment
Configuring a new profile for LambdaSharp tool
Tool profile name: [Default]
Existing S3 bucket name for module deployments (blank value creates new bucket):
S3 bucket path for module deployments: [Modules/]
Existing SNS topic ARN for CloudFormation notifications (empty value creates new bucket):
=> Stack creation initiated for LambdaSharpTool-Demo
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              LambdaSharpTool-Demo (User Initiated)
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     VersionSsmParameter
CREATE_IN_PROGRESS                  AWS::S3::Bucket                                         DeploymentBucket
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         DeploymentNotificationTopic
...
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter (Resource creation Initiated)
CREATE_COMPLETE                     AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              LambdaSharpTool-Demo
=> Stack creation finished (finished: 2018-10-25 21:23:05)

Done (duration: 00:00:40.0739292)
```

### Configure a new tool profile without any prompts

__Using Powershell/Bash:__
```bash
dotnet lash config --tool-profile Demo --module-s3-bucket-name="" --module-s3-bucket-path="Modules/" --cloudformation-notifications-topic=""
```

Output:
```
MindTouch LambdaSharp Tool (v0.4-WIP) - Configure LambdaSharp environment
Configuring a new profile for LambdaSharp tool
Creating tool profile: Demo
Creating new S3 bucket
Using S3 bucket path: Modules/
Creating new SNS topic for CloudFormation notifications
=> Stack creation initiated for LambdaSharpTool-Demo
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              LambdaSharpTool-Demo (User Initiated)
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         DeploymentNotificationTopic
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketPathSsmParameter
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     StackNameSsmParameter
...
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_IN_PROGRESS                  AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter (Resource creation Initiated)
CREATE_COMPLETE                     AWS::SSM::Parameter                                     DeploymentBucketNameSsmParameter
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              LambdaSharpTool-Demo
=> Stack creation finished (finished: 2018-10-25 22:05:20)

Done (duration: 00:00:36.3661853)
```

