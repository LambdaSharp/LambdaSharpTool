![λ#](../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp Tool

The λ# tool is used to process the deployment file, compile the C# projects, upload their packages to S3, generate a CloudFormation stack, and then create or update it. All operations are done in a single pass to facilitate greater productivity when building new λ# apps. In addition, λ# uses a deterministic build process which enables it to skip updates when no code or configuration changes occurred since the last deployment.

Commands:

1. [Deploy](#deploy-command)
1. [Info](#info-command)
1. [New Function](#new-function-command)

## Deploy Command

The `deploy` command parses the input file, compiles all included function projects, and deploys the changes to the AWS account.

The default filename for the deployment file is `Deploy.yml` in the current working directory. If the file has a different name or is not in the current directory, it must be specified on the command line using the `--input` option.

CloudFormation stacks created by the λ# tool have termination protection enabled. In addition, subsequent updates cannot delete or replace data resources unless the `--allow-data-loss` option is passed in. This behavior is to reduce the risk of accidental data loss when CloudFormation resources are being accidentally replaced.

```
> lst deploy --deployment MyDeployment
MindTouch LambdaSharp Tool - Deploy LambdaSharp app
Retrieving LambdaSharp settings for `MyDeployment'
Loading 'Deploy.yml'
Pre-processing
Analyzing
Building function Sample.SlackCommand [netcoreapp2.0]
=> Restoring project dependencies
=> Building AWS Lambda package
=> Decompressing AWS Lambda package
=> Adding settings file 'parameters.json'
=> Finalizing AWS Lambda package
Deploying stack: MyDeployment-Sample
=> Uploading CloudFormation template: s3://mydeployment-lambdasharp-deploymentbucket/MyDeployment/Sample/cloudformation-8ec32d267a1fef38e8e133d8ee19cf857d3a0911.json => Stack creation initiated
...
=> Stack creation finished
```

### Options

<dl>
<dt><tt>--input &lt;FILE></tt></dt>
<dd>(optional) YAML app deployment file (default: Deploy.yml)</dd>
<dt><tt>--dryrun[:&lt;LEVEL&gt;]</tt></dt>
<dd>(optional) Generate output assets without deploying (0=everything, 1=cloudformation)</dd>
<dt><tt>--output &lt;FILE&gt;</tt></dt>
<dd>(optional) Name of generated CloudFormation template file (default: cloudformation.json)</dd>
<dt><tt>--allow-data-loss</tt></dt>
<dd>(optional) Allow CloudFormation resource update operations that could lead to data loss</dd>
<dt><tt>--deployment|-D &lt;NAME&gt;</tt></dt>
<dd>(optional) Name of deployment (default: <tt>LAMBDASHARPDEPLOYMENT</tt> environment variable)</dd>
<dt><tt>--profile|-P &lt;NAME&gt;</tt></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>
<dt><tt>--verbose|-V[:&lt;LEVEL&gt;]</tt></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>
<dt><tt>--gitsha <&lt;VALUE&gt;</tt></dt>
<dd>(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)</dd>
<dt><tt>--aws-account-id &lt;VALUE&gt;</tt></dt>
<dd>(test only) Override AWS account Id (default: read from AWS profile)</dd>
<dt><tt>--aws-region &lt;NAME&gt;</tt></dt>
<dd>(test only) Override AWS region (default: read from AWS profile)</dd>
<dt><tt>--deployment-bucket-name &lt;NAME&gt;</tt></dt>
<dd>(test only) S3 Bucket used to deploying assets (default: read from LambdaSharp configuration)</dd>
<dt><tt>--deployment-deadletter-queue-url &lt;URL&gt;</tt></dt>
<dd>(test only) SQS Deadletter queue used by function (default: read from LambdaSharp configuration)</dd>
<dt><tt>--deployment-notification-topic-arn &lt;ARN&gt;</tt></dt>
<dd>(test only) SNS Topic used by CloudFormation deploymetions (default: read from LambdaSharp configuration)</dd>
<dt><tt>--bootstrap</tt></dt>
<dd>(boostrap only) Don't read LambdaSharp initialization values</dd>
</dl>

## Info Command

The `info` command shows the deployment settings for λ# apps.

The following settings are read from AWS Systems Manager Parameter Store:
<dl>
<dt><tt>/{Deployment}/LambdaSharp/DeadLetterQueue</tt></dt>
<dd>The SQS Queue URL used by Lambda functions as their dead-letter queue.</dd>
<dt><tt>/{Deployment}/LambdaSharp/DeploymentBucket</tt></dt>
<dd>The S3 bucket used by the λ# tool to upload assets.</dd>
<dt><tt>/{Deployment}/LambdaSharp/DeploymentNotificationTopic</tt></dt>
<dd>(optiona) The ARN for an SNS topic that will be used to broadcast stack creation, update, and deletion events.</dd>
<dt><tt>/{Deployment}/LambdaSharp/RollbarCustomResourceTopic</tt></dt>
<dd>(optional) The ARN for an SNS topic that will create a Rollbar project and return its tokwn.</dd>
</dl>


```
> lst info --deployment MyDeployment
MindTouch LambdaSharp Tool - Show LambdaSharp settings
Retrieving LambdaSharp settings for `MyDeployment'
Deployment: MyDeployment
Git SHA: 8ec32d267a1fef38e8e133d8ee19cf857d3a0911
AWS Region: us-east-1
AWS Account Id: 123456789012
LambdaSharp S3 Bucket: mydeployment-lambdasharp-deploymentbucket
LambdaSharp Dead-Letter Queue: https://sqs.us-east-1.amazonaws.com/mydeployment/MyDeployment-LambdaSharp-DeadLetterQueue
LambdaSharp CloudFormation Notification Topic: arn:aws:sns:us-east-1:123456789012:MyDeployment-LambdaSharp-DeploymentNotificationTopic
LambdaSharp Rollbar Custom Resource Topic: arn:aws:sns:us-east-1:123456789012:LambdaSharpRollbar-RollbarCustomResourceTopic
```

### Options

<dl>
<dt><tt>--deployment|-D &lt;NAME&gt;</tt></dt>
<dd>(optional) Name of deployment (default: <tt>LAMBDASHARPDEPLOYMENT</tt> environment variable)</dd>
<dt><tt>--profile|-P &lt;NAME&gt;</tt></dt>
<dd>(optional) Use a specific AWS profile from the AWS credentials file</dd>
<dt><tt>--verbose|-V[:&lt;LEVEL&gt;]</tt></dt>
<dd>(optional) Show verbose output (0=quiet, 1=normal, 2=detailed, 3=exceptions)</dd>
<dt><tt>--gitsha <&lt;VALUE&gt;</tt></dt>
<dd>(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)</dd>
<dt><tt>--aws-account-id &lt;VALUE&gt;</tt></dt>
<dd>(test only) Override AWS account Id (default: read from AWS profile)</dd>
<dt><tt>--aws-region &lt;NAME&gt;</tt></dt>
<dd>(test only) Override AWS region (default: read from AWS profile)</dd>
<dt><tt>--deployment-bucket-name &lt;NAME&gt;</tt></dt>
<dd>(test only) S3 Bucket used to deploying assets (default: read from LambdaSharp configuration)</dd>
<dt><tt>--deployment-deadletter-queue-url &lt;URL&gt;</tt></dt>
<dd>(test only) SQS Deadletter queue used by function (default: read from LambdaSharp configuration)</dd>
<dt><tt>--deployment-notification-topic-arn &lt;ARN&gt;</tt></dt>
<dd>(test only) SNS Topic used by CloudFormation deploymetions (default: read from LambdaSharp configuration)</dd>
<dt><tt>--bootstrap</tt></dt>
<dd>(boostrap only) Don't read LambdaSharp initialization values</dd>
</dl>

## New Function Command

The `new function` command creates a new C# project in the current folder with the required dependencies, as well as a `Function.cs` file with a skeleton AWS Lambda implementation.

```
> lst new function --name MyApp.MyFunction --namespace MyCompany.MyApp.MyFunction
MindTouch LambdaSharp Tool - Create new LambdaSharp asset
Created project file: MyApp.MyFunction/MyApp.MyFunction.csproj
Created function file: MyApp.MyFunction/Function.cs
```

### Options

<dl>
<dt><tt>--name|-n &lt;VALUE&gt;</tt></dt>
<dd>Name of new project (e.g. App.Function)</dd>
<dt><tt>--namespace|-ns &lt;VALLUE&gt;</tt></dt>
<dd>(optional) Root namespace for project (default: same as function name)</dd>
<dt><tt>--working-directory|-wd &lt;VALUE&gt;</tt></dt>
<dd>(optional) New function project parent directory (default: current directory)</dd>
<dt><tt>--framework|-f &lt;VALUE&gt;</tt></dt>
<dd>(optional) Target .NET framework (default: 'netcoreapp2.0')</dd>
</dl>
