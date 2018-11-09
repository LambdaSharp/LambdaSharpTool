![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - Deploy Command

The `deploy` command is used to deploy a published module.

CloudFormation stacks created by the λ# CLI have termination protection enabled when deployed with the `--protect` option. In addition, subsequent updates cannot delete or replace data resources unless the `--allow-data-loss` option is passed in. This behavior is to reduce the risk of accidental data loss when CloudFormation resources are being accidentally replaced.

## Arguments

The `deploy` command takes an optional argument. The argument can either be the name of a published module with an optional version constraint, a path to a manifest file, a path to a module definition, or a path to a folder containing a `Module.yml` file.

If the argument refers to a manifest file, the `deploy` command invokes `publish` command to upload the module and its assets to the deployment bucket.

If the argument refers to a module definition, the `deploy` command invokes the `build` command to compile the module and all its assets, followed by the `publish` command to upload all built assets.

## Options

<dl>

<dt><code>--name &lt;NAME&gt;</code></dt>
<dd>(optional) Specify an alternative module name for the deployment (default: module name)</dd>

<dt><code>--inputs &lt;FILE&gt;</code></dt>
<dd>(optional) Specify filename to read module inputs from (default: none)</dd>

<dt><code>--input|-KV &lt;KEY&gt;=&lt;VALUE&gt;</code></dt>
<dd>(optional) Specify module input key-value pair (can be used multiple times)</dd>

<dt><code>--allow-data-loss</code></dt>
<dd>(optional) Allow CloudFormation resource update operations that could lead to data loss</dd>

<dt><code>--protect</code></dt>
<dd>(optional) Enable termination protection for the CloudFormation stack</dd>

<dt><code>--force-deploy</code></dt>
<dd>(optional) Force module deployment</dd>

<dt><code>--skip-assembly-validation</code></dt>
<dd>(optional) Disable validating LambdaSharp assembly references in function project files</dd>

<dt><code>-c|--configuration &lt;CONFIGURATION&gt;</code></dt>
<dd>(optional) Build configuration for function projects (default: "Release")</dd>

<dt><code>--gitsha &lt;VALUE&gt;</code></dt>
<dd>(optional) GitSha of most recent git commit (default: invoke `git rev-parse HEAD` command)</dd>

<dt><code> -o|--output &lt;DIRECTORY&gt;</code></dt>
<dd>(optional) Path to output directory (default: bin)</dd>

<dt><code>-selector &lt;NAME&gt;</code></dt>
<dd>(optional) Selector for resolving conditional compilation choices in module</dd>

<dt><code>--dryrun[:&lt;LEVEL&gt;]</code></dt>
<dd>(optional) Generate output assets without deploying (0=everything, 1=cloudformation)</dd>

<dt><code>--cf-output &lt;FILE&gt;</code></dt>
<dd>(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)</dd>

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

### Build, publish, and deploy module in current folder

__Using Powershell/Bash:__
```bash
dotnet lash deploy
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Deploy LambdaSharp module
Readying module for deployment tier 'Sandbox'

Compiling module: Module.yml
Building function RecordMessage [netcoreapp2.1, Release]
=> Restoring project dependencies
=> Building AWS Lambda package
=> Decompressing AWS Lambda package
=> Finalizing AWS Lambda package
Building function SlackCommand [netcoreapp2.1, Release]
=> Restoring project dependencies
=> Building AWS Lambda package
=> Decompressing AWS Lambda package
=> Finalizing AWS Lambda package
=> Module compilation done
Publishing module: Demo
=> Uploading function: s3://lambdasharp-bucket-name/Modules/Demo/Assets/function_RecordMessage_4E05BDFA74DAC87A05165A4D5B609B39.zip
=> Uploading function: s3://lambdasharp-bucket-name/Modules/Demo/Assets/function_SlackCommand_8207022C95970006F597FF6060366C34.zip
=> Uploading template: s3://lambdasharp-bucket-name/Modules/Demo/Assets/cloudformation_v1.0_F2E9DA098FB8B0EB118F5839947467A6.json
=> Uploading manifest: s3://lambdasharp-bucket-name/Modules/Demo/Assets/manifest_v1.0_F2E9DA098FB8B0EB118F5839947467A6.json
Deploying stack: Sandbox-Demo [Demo]
=> Stack creation initiated for Sandbox-Demo
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-Demo (User Initiated)
CREATE_IN_PROGRESS                  AWS::ApiGateway::RestApi                                ModuleRestApi
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         SnsTopic
CREATE_IN_PROGRESS                  AWS::IAM::Role                                          CloudWatchLogsRole
...
CREATE_IN_PROGRESS                  AWS::ApiGateway::UsagePlan                              UsagePlan
CREATE_IN_PROGRESS                  AWS::ApiGateway::UsagePlan                              UsagePlan (Resource creation Initiated)
CREATE_COMPLETE                     AWS::ApiGateway::UsagePlan                              UsagePlan
CREATE_IN_PROGRESS                  Custom::LambdaSharpRegisterFunction                     RecordMessageRegistration (Resource creation Initiated)
CREATE_COMPLETE                     Custom::LambdaSharpRegisterFunction                     RecordMessageRegistration
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              Sandbox-Demo
=> Stack creation finished (finished: 2018-10-26 17:50:26)
Stack output values:
=> ModuleName: Demo
=> ModuleVersion: 1.0
=> The topic that the lambda function subscribes to record messages: arn:aws:sns:us-east-1:123456789012:Sandbox-Demo-SnsTopic-NEWOC7GZD51
=> The topic that the lambda function subscribes to record messages: arn:aws:sns:us-east-1:123456789012:Sandbox-Demo-SnsTopic-NEWOC7GZD51

Done (duration: 00:01:55.2782614)
```

### Deploy a published module

__Using Powershell/Bash:__
```bash
dotnet lash deploy Demo/bin/manifest.json
```

Output:
```
MindTouch LambdaSharp CLI (v0.4) - Deploy LambdaSharp module
Readying module for deployment tier 'Sandbox'
Publishing module: Demo
=> Uploading function: s3://lambdasharp-bucket-name/Modules/Demo/Assets/function_RecordMessage_4E05BDFA74DAC87A05165A4D5B609B39.zip
=> Uploading function: s3://lambdasharp-bucket-name/Modules/Demo/Assets/function_SlackCommand_8207022C95970006F597FF6060366C34.zip
=> Uploading template: s3://lambdasharp-bucket-name/Modules/Demo/Assets/cloudformation_v1.0_F2E9DA098FB8B0EB118F5839947467A6.json
=> Uploading manifest: s3://lambdasharp-bucket-name/Modules/Demo/Assets/manifest_v1.0_F2E9DA098FB8B0EB118F5839947467A6.json
Deploying stack: Sandbox-Demo [Demo]
=> Stack creation initiated for Sandbox-Demo
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              Sandbox-Demo (User Initiated)
CREATE_IN_PROGRESS                  AWS::SNS::Topic                                         SnsTopic
CREATE_IN_PROGRESS                  AWS::DynamoDB::Table                                    MessageTable
CREATE_IN_PROGRESS                  AWS::ApiGateway::RestApi                                ModuleRestApi
...
CREATE_IN_PROGRESS                  AWS::ApiGateway::UsagePlan                              UsagePlan (Resource creation Initiated)
CREATE_COMPLETE                     AWS::ApiGateway::UsagePlan                              UsagePlan
CREATE_COMPLETE                     AWS::Lambda::Permission                                 RecordMessageSnsTopicSnsPermission
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              Sandbox-Demo
=> Stack creation finished (finished: 2018-10-26 18:54:43)
Stack output values:
=> ModuleName: Demo
=> ModuleVersion: 1.0
=> The topic that the lambda function subscribes to record messages: arn:aws:sns:us-east-1:123456789012:Sandbox-Demo-SnsTopic-101QRCI4FPHD9
=> The topic that the lambda function subscribes to record messages: arn:aws:sns:us-east-1:123456789012:Sandbox-Demo-SnsTopic-101QRCI4FPHD9

Done (duration: 00:01:21.7651019)
```

### Deploy a module with an inputs file

The `deploy` command can optionally take a YAML file to specify the parameter values. The YAML file must be a map of key-value pairs, where each key corresponds to a parameter or import. The value can either be a literal value (string, number, boolean) or a list. Lists are automatically concatenated into a comma-separated string of values.

The `Secrets` key has some additional, special processing rules. `Secrets` is used to enable a module to use additional managed encryption keys. These can be specified with an account specified key ID or with an account-agnostic key alias. When a key alias is used, the `deploy` command automatically resolves it to a key ID before using it as a parameter value.

```yaml
ParameterValue: parameter value
ParameterCommaSeparatedList:
    - first value
    - second value
Secrets:
    - alias/MySecretKey
```

__Using Powershell/Bash:__
```bash
dotnet lash deploy --inputs inputs.yml Demo
```
