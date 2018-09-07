![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp Tool

The λ# tool is used to process the module file, compile the C# projects, upload their packages to S3, generate a CloudFormation stack, and then create or update it. All operations are done in a single pass to facilitate greater productivity when building new λ# module. In addition, λ# uses a deterministic build process which enables it to skip updates when no code or configuration changes have occurred since the last deployment operation.

Commands:

1. [Deploy](#deploy-command)
1. [Info](#info-command)
1. [New Function](#new-function-command)

## Deploy Command

The `deploy` command parses the input file, compiles all included function projects, and deploys the changes to the AWS account.

The default filename for the module file is `Module.yml` in the current working directory. If the file has a different name or is not in the current directory, it must be specified as an argument on the command line.

CloudFormation stacks created by the λ# tool have termination protection enabled when deployed with the `--protect` option. In addition, subsequent updates cannot delete or replace data resources unless the `--allow-data-loss` option is passed in. This behavior is to reduce the risk of accidental data loss when CloudFormation resources are being accidentally replaced.

```
> lash deploy --tier Demo
MindTouch LambdaSharp Tool - Deploy LambdaSharp module
Loading 'Module.yml'
Pre-processing
Analyzing
Building function Sample.SlackCommand [netcoreapp2.0]
=> Restoring project dependencies
=> Building AWS Lambda package
=> Decompressing AWS Lambda package
=> Adding settings file 'parameters.json'
=> Finalizing AWS Lambda package
Deploying stack: Demo-Sample
=> Uploading CloudFormation template: s3://demo-lambdasharp-deploymentbucket/Demo/Sample/cloudformation-8ec32d267a1fef38e8e133d8ee19cf857d3a0911.json => Stack creation initiated
...
=> Stack creation finished
```

### Argument

The path to the YAML module file can be optionally specified as an argument. When omitted, the tool will look for a file called `Module.yml`.

```
lash deploy Module.yml
```

### Options

<dl>
<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>(optional) Name of deployment tier (default: <code>LAMBDASHARPTIER</code> environment variable)</dd>
<dt><code>--dryrun[:&lt;LEVEL&gt;]</code></dt>
<dd>(optional) Generate output assets without deploying (0=everything, 1=cloudformation)</dd>
<dt><code>--output &lt;FILE&gt;</code></dt>
<dd>(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)</dd>
<dt><code>--allow-data-loss</code></dt>
<dd>(optional) Allow CloudFormation resource update operations that could lead to data loss</dd>
<dt><code>--protect</code></dt>
<dd>(optional) Enable termination protection for the CloudFormation stack</dd>
<dt><code>-c|--configuration &lt;CONFIGURATION&gt;</code></dt>
<dd>(optional) Build configuration for function projects (default: "Release")</dd>
<dt><code>--profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>
<dt><code>--verbose|-V[:&lt;LEVEL&gt;]</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>
<dt><code>--gitsha &lt;VALUE&gt;</code></dt>
<dd>(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)</dd>
<dt><code>--aws-account-id &lt;VALUE&gt;</code></dt>
<dd>(test only) Override AWS account Id (default: read from AWS profile)</dd>
<dt><code>--aws-region &lt;NAME&gt;</code></dt>
<dd>(test only) Override AWS region (default: read from AWS profile)</dd>
<dt><code>--deployment-version &lt;VERSION&gt;</code></dt>
<dd>(test only) LambdaSharp environment version for deployment tier (default: read from LambdaSharp configuration)</dd>
<dt><code>--deployment-bucket-name &lt;NAME&gt;</code></dt>
<dd>(test only) S3 Bucket used to deploying assets (default: read from LambdaSharp configuration)</dd>
<dt><code>--deployment-deadletter-queue-url &lt;URL&gt;</code></dt>
<dd>(test only) SQS Deadletter queue used by function (default: read from LambdaSharp configuration)</dd>
<dt><code>--deployment-logging-topic-arn &lt;ARN&gt;</code></dt>
<dd>(test only) SNS topic used by LambdaSharp functions to log warnings and errors (default: read from LambdaSharp configuration)</dd>
<dt><code>--deployment-notification-topic-arn &lt;ARN&gt;</code></dt>
<dd>(test only) SNS Topic used by CloudFormation deployments (default: read from LambdaSharp configuration)</dd>
</dl>

## Info Command

The `info` command shows the settings for λ# modules.

The following settings are read from AWS Systems Manager Parameter Store:
<dl>
<dt><code>/{{Tier}}/LambdaSharp/DeadLetterQueue</code></dt>
<dd>The SQS Queue URL used by Lambda functions as their dead-letter queue.</dd>
<dt><code>/{{Tier}}/LambdaSharp/DeploymentBucket</code></dt>
<dd>The S3 bucket used by the λ# tool to upload assets.</dd>
<dt><code>/{{Tier}}/LambdaSharp/DeploymentNotificationTopic</code></dt>
<dd>(optional) The ARN for an SNS topic that will be used to broadcast stack creation, update, and deletion events.</dd>
<dt><code>/{{Tier}}/LambdaSharp/RollbarCustomResourceTopic</code></dt>
<dd>(optional) The ARN for an SNS topic that will create a Rollbar project and return its tokwn.</dd>
</dl>


```
> lash info --tier Demo
MindTouch LambdaSharp Tool - Show LambdaSharp settings
Deployment tier: Demo
Git SHA: 8ec32d267a1fef38e8e133d8ee19cf857d3a0911
AWS Region: us-east-1
AWS Account Id: 123456789012
LambdaSharp Environment Version: 0.3
LambdaSharp S3 Bucket: demo-lambdasharp-deploymentbucket
LambdaSharp Dead-Letter Queue: https://sqs.us-east-1.amazonaws.com/Demo-LambdaSharp-DeadLetterQueue
LambdaSharp Logging Topic: arn:aws:sns:us-east-1:123456789012:Demo-LambdaSharp-LoggingTopic
LambdaSharp CloudFormation Notification Topic: arn:aws:sns:us-east-1:123456789012:Demo-LambdaSharp-DeploymentNotificationTopic
LambdaSharp Rollbar Custom Resource Topic: arn:aws:sns:us-east-1:123456789012:LambdaSharpRollbar-RollbarCustomResourceTopic
```

### Options

<dl>
<dt><code>--tier|-T &lt;NAME&gt;</code></dt>
<dd>(optional) Name of deployment tier (default: <code>LAMBDASHARPTIER</code> environment variable)</dd>
<dt><code>--profile|-P &lt;NAME&gt;</code></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>
<dt><code>--verbose|-V[:&lt;LEVEL&gt;]</code></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>
<dt><code>--gitsha &lt;VALUE&gt;</code></dt>
<dd>(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)</dd>
<dt><code>--aws-account-id &lt;VALUE&gt;</code></dt>
<dd>(test only) Override AWS account Id (default: read from AWS profile)</dd>
<dt><code>--aws-region &lt;NAME&gt;</code></dt>
<dd>(test only) Override AWS region (default: read from AWS profile)</dd>
<dt><code>--deployment-version &lt;VERSION&gt;</code></dt>
<dd>(test only) LambdaSharp environment version for deployment tier (default: read from LambdaSharp configuration)</dd>
<dt><code>--deployment-bucket-name &lt;NAME&gt;</code></dt>
<dd>(test only) S3 Bucket used to deploying assets (default: read from LambdaSharp configuration)</dd>
<dt><code>--deployment-deadletter-queue-url &lt;URL&gt;</code></dt>
<dd>(test only) SQS Deadletter queue used by function (default: read from LambdaSharp configuration)</dd>
<dt><code>--deployment-logging-topic-arn &lt;ARN&gt;</code></dt>
<dd>(test only) SNS topic used by LambdaSharp functions to log warnings and errors (default: read from LambdaSharp configuration)</dd>
<dt><code>--deployment-notification-topic-arn &lt;ARN&gt;</code></dt>
<dd>(test only) SNS Topic used by CloudFormation deployments (default: read from LambdaSharp configuration)</dd>
</dl>

## New Function Command

The `new function` command creates a new C# project in the current folder with the required dependencies, as well as a `Function.cs` file with a skeleton AWS Lambda implementation.

```
> lash new function --name MyApp.MyFunction --namespace MyCompany.MyApp.MyFunction
MindTouch LambdaSharp Tool - Create new LambdaSharp asset
Created project file: MyApp.MyFunction/MyApp.MyFunction.csproj
Created function file: MyApp.MyFunction/Function.cs
```

### Options

<dl>
<dt><code>--name|-n &lt;VALUE&gt;</code></dt>
<dd>Name of new project (e.g. Module.Function)</dd>
<dt><code>--namespace|-ns &lt;VALUE&gt;</code></dt>
<dd>(optional) Root namespace for project (default: same as function name)</dd>
<dt><code>--working-directory|-wd &lt;VALUE&gt;</code></dt>
<dd>(optional) New function project parent directory (default: current directory)</dd>
<dt><code>--framework|-f &lt;VALUE&gt;</code></dt>
<dd>(optional) Target .NET framework (default: 'netcoreapp2.0')</dd>
</dl>
