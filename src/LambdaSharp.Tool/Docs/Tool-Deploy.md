![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Deploy Command

The `deploy` command is used to deploy a published module.

CloudFormation stacks created by the λ# CLI have termination protection enabled when deployed with the `--protect` option. In addition, subsequent updates cannot delete or replace data resources unless the `--allow-data-loss` option is passed in. This behavior is to reduce the risk of accidental data loss when CloudFormation resources are replaced.

## Arguments

The `deploy` command takes an optional argument. The argument can either be the name of a published module with an optional version constraint, a path to a manifest file, a path to a module definition, or a path to a folder containing a `Module.yml` file.

If the argument refers to a manifest file, the `deploy` command invokes `publish` command to upload the module and its assets to the deployment bucket.

If the argument refers to a module definition, the `deploy` command invokes the `build` command to compile the module and all its assets, followed by the `publish` command to upload all built assets.

## Options

<dl>

<dt><code>--name &lt;NAME&gt;</code></dt>
<dd>(optional) Specify an alternative module name for the deployment (default: module name)</dd>

<dt><code>--parameters &lt;FILE&gt;</code></dt>
<dd>(optional) Specify filename to read module parameters from (default: none)</dd>

<dt><code>--allow-data-loss</code></dt>
<dd>(optional) Allow CloudFormation resource update operations that could lead to data loss</dd>

<dt><code>--protect</code></dt>
<dd>(optional) Enable termination protection for the CloudFormation stack</dd>

<dt><code>--xray</code></dt>
<dd>(optional) Enable service-call tracing with AWS X-Ray for all functions in module</dd>

<dt><code>--force-deploy</code></dt>
<dd>(optional) Force module deployment</dd>

<dt><code>--prompt-all</code></dt>
<dd>(optional) Prompt for all missing parameters values (default: only prompt for missing parameters with no default value)</dd>

<dt><code>--prompts-as-errors</code></dt>
<dd>(optional) Missing parameters cause an error instead of a prompts (use for CI/CD to avoid unattended prompts)</dd>

<dt><code>--force-publish</code></dt>
<dd>(optional) Publish modules and their assets even when no changes were detected</dd>

<dt><code>--no-assembly-validation</code></dt>
<dd>(optional) Disable validating LambdaSharp assembly references in function project files</dd>

<dt><code>--no-dependency-validation</code></dt>
<dd>(optional) Disable validating LambdaSharp module dependencies</dd>

<dt><code>--configuration|-c &lt;CONFIGURATION&gt;</code></dt>
<dd>(optional) Build configuration for function projects (default: "Release")</dd>

<dt><code>--git-sha &lt;VALUE&gt;</code></dt>
<dd>(optional) Git SHA of most recent git commit (default: invoke `git rev-parse HEAD` command)</dd>

<dt><code>--git-branch &lt;VALUE&gt;</code></dt>
<dd>(optional) (optional) Git branch name (default: invoke `git rev-parse --abbrev-ref HEAD` command)</dd>

<dt><code>--output|-o  &lt;DIRECTORY&gt;</code></dt>
<dd>(optional) Path to output directory (default: bin)</dd>

<dt><code>--selector &lt;NAME&gt;</code></dt>
<dd>(optional) Selector for resolving conditional compilation choices in module</dd>

<dt><code>--dryrun[:&lt;LEVEL&gt;]</code></dt>
<dd>(optional) Generate output assets without deploying (0=everything, 1=cloudformation)</dd>

<dt><code>--cfn-output &lt;FILE&gt;</code></dt>
<dd>(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)</dd>

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

### Build, publish, and deploy module in current folder

__Using PowerShell/Bash:__
```bash
lash deploy
```

Output:
```
LambdaSharp CLI (v0.5) - Deploy LambdaSharp module
Readying module for deployment tier 'Sandbox'

Compiling module: Module.yml
=> Building function RecordMessage [netcoreapp2.1, Release]
=> Building function SlackCommand [netcoreapp2.1, Release]
=> Module compilation done: C:\LambdaSharpTool\Demos\Demo\bin\cloudformation.json
Publishing module: LambdaSharp.Demo
=> Uploading asset: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/function_RecordMessage_8A0A4D0DA5B090BD33D779EF16FE7470.zip
=> Uploading asset: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/function_SlackCommand_30C238770176A7AE6955A519FC6A283A.zip
=> Uploading template: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/cloudformation_v1.0-DEV_A556D13161D90F32959CDE5EC121B7D0.json
Resolving module reference: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Versions/1.0-DEV/cloudformation.json
=> Validating module for deployment tier

Deploying stack: Sandbox-LambdaSharp-Demo [LambdaSharp.Demo:1.0-DEV]
=> Stack create initiated for Sandbox-LambdaSharp-Demo [CAPABILITY_IAM]
REVIEW_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo (User Initiated)
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo (User Initiated)
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         NotifyRecorderTopic
CREATE_IN_PROGRESS                  AWS::DynamoDB::Table                                    MessageTable
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         NotifyRecorderTopic (Resource creation Initiated)
...
CREATE_COMPLETE                     AWS::ApiGateway::UsagePlan                              UsagePlan
CREATE_IN_PROGRESS                  LambdaSharp::Registration::Function                     SlackCommand::Registration (Resource creation Initiated)
CREATE_COMPLETE                     LambdaSharp::Registration::Function                     SlackCommand::Registration
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo
=> Stack create finished
Stack output values:
=> Module: LambdaSharp.Demo:1.0-DEV
=> The topic for recording messages: arn:aws:sns:us-east-1:123456789012:Sandbox-LambdaSharp-Demo-NotifyRecorderTopic-69KFERDLAQIW

Done (finished: 1/17/2019 4:12:13 PM; duration: 00:02:23.5535481)
```

### Deploy a published module

__Using PowerShell/Bash:__
```bash
lash deploy Demo/bin/manifest.json
```

Output:
```
LambdaSharp CLI (v0.5) - Deploy LambdaSharp module
Readying module for deployment tier 'Sandbox'

Publishing module: LambdaSharp.Demo
=> Uploading asset: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/function_RecordMessage_8A0A4D0DA5B090BD33D779EF16FE7470.zip
=> Uploading asset: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/function_SlackCommand_30C238770176A7AE6955A519FC6A283A.zip
=> Uploading template: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Assets/cloudformation_v1.0-DEV_A556D13161D90F32959CDE5EC121B7D0.json
Resolving module reference: s3://lambdasharp-bucket-name/LambdaSharp/Modules/Demo/Versions/1.0-DEV/cloudformation.json
=> Validating module for deployment tier

Deploying stack: Sandbox-LambdaSharp-Demo [LambdaSharp.Demo:1.0-DEV]
=> Stack create initiated for Sandbox-LambdaSharp-Demo [CAPABILITY_IAM]
REVIEW_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo (User Initiated)
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo (User Initiated)
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         NotifyRecorderTopic
CREATE_IN_PROGRESS                  AWS::DynamoDB::Table                                    MessageTable
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         NotifyRecorderTopic (Resource creation Initiated)
...
CREATE_COMPLETE                     AWS::ApiGateway::UsagePlan                              UsagePlan
CREATE_IN_PROGRESS                  LambdaSharp::Registration::Function                     SlackCommand::Registration (Resource creation Initiated)
CREATE_COMPLETE                     LambdaSharp::Registration::Function                     SlackCommand::Registration
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              Sandbox-LambdaSharp-Demo
=> Stack create finished
Stack output values:
=> Module: LambdaSharp.Demo:1.0-DEV
=> The topic for recording messages: arn:aws:sns:us-east-1:123456789012:Sandbox-LambdaSharp-Demo-NotifyRecorderTopic-69KFERDLAQIW

Done (finished: 1/17/2019 4:12:13 PM; duration: 00:02:23.5535481)
```

### Deploy a module with a parameters file

The `deploy` command can optionally take a YAML file to specify the parameter values. The YAML file must be a map of key-value pairs, where each key corresponds to a parameter or import name. The value can either be a literal value (string, number, boolean) or a list. Lists are automatically concatenated into a comma-separated string of values.

The `Secrets` key has some additional special processing rules. `Secrets` is used to enable a module to use additional managed encryption keys. These can be specified with an account specified key ID or with an account-agnostic key alias. When a key alias is used, the `deploy` command automatically resolves it to a key ID before using it as a parameter value.

```yaml
ParameterValue: parameter value
ParameterCommaSeparatedList:
    - first value
    - second value
Secrets:
    - alias/MySecretKey
```

__Using PowerShell/Bash:__
```bash
lash deploy --inputs inputs.yml Demo
```
