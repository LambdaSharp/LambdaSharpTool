---
title: LambdaSharp CLI - Deploy Command
description: Deploy a published LambdaSharp module to a deployment tier
keywords: cli, build, publish, deploy, deployment, tier, module
---
# Deploy Module

The `deploy` command is used to deploy a published module.

CloudFormation stacks created by the LambdaSharp CLI have termination protection enabled when deployed with the `--protect` option. In addition, subsequent updates cannot delete or replace data resources unless the `--allow-data-loss` option is passed in. This behavior is to reduce the risk of accidental data loss when CloudFormation resources are replaced.

## Arguments

The `deploy` command takes an optional argument. The argument can either be the name of a published module with an optional version constraint, a path to a manifest file, a path to a module definition, or a path to a folder containing a `Module.yml` file.

If the argument refers to a manifest file, the `deploy` command invokes `publish` command to upload the module and its artifacts to the deployment bucket.

If the argument refers to a module definition, the `deploy` command invokes the `build` command to compile the module and all its artifacts, followed by the `publish` command to upload all built artifacts.

## Options

<dl>

<dt><code>--name &lt;NAME&gt;</code></dt>
<dd>

(optional) Specify an alternative module name for the deployment (default: module name)
</dd>

<dt><code>--parameters &lt;FILE&gt;</code></dt>
<dd>

(optional) Specify filename to read module parameters from (default: none)
</dd>

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

<dt><code>--force-deploy</code></dt>
<dd>

(optional) Force module deployment
</dd>

<dt><code>--prompt-all</code></dt>
<dd>

(optional) Prompt for all missing parameters values (default: only prompt for missing parameters with no default value)
</dd>

<dt><code>--prompts-as-errors</code></dt>
<dd>

(optional) Missing parameters cause an error instead of a prompts (use for CI/CD to avoid unattended prompts)
</dd>

<dt><code>--force-publish</code></dt>
<dd>

(optional) Publish modules and their artifacts even when no changes were detected
</dd>

<dt><code>--no-assembly-validation</code></dt>
<dd>

(optional) Disable validating LambdaSharp assembly references in function project files
</dd>

<dt><code>--no-dependency-validation</code></dt>
<dd>

(optional) Disable validating LambdaSharp module dependencies
</dd>

<dt><code>--configuration|-c &lt;CONFIGURATION&gt;</code></dt>
<dd>

(optional) Build configuration for function projects (default: "Release")
</dd>

<dt><code>--git-sha &lt;VALUE&gt;</code></dt>
<dd>

(optional) Git SHA of most recent git commit (default: invoke `git rev-parse HEAD` command)
</dd>

<dt><code>--git-branch &lt;VALUE&gt;</code></dt>
<dd>

(optional) (optional) Git branch name (default: invoke `git rev-parse --abbrev-ref HEAD` command)
</dd>

<dt><code>--output|-o  &lt;DIRECTORY&gt;</code></dt>
<dd>

(optional) Path to output directory (default: bin)
</dd>

<dt><code>--selector &lt;NAME&gt;</code></dt>
<dd>

(optional) Selector for resolving conditional compilation choices in module
</dd>

<dt><code>--dryrun[:&lt;LEVEL&gt;]</code></dt>
<dd>

(optional) Generate output artifacts without deploying (0=everything, 1=cloudformation)
</dd>

<dt><code>--cfn-output &lt;FILE&gt;</code></dt>
<dd>

(optional) Name of generated CloudFormation template file (default: bin/cloudformation.json)
</dd>

<dt><code>--module-version &lt;VERSION&gt;</code></dt>
<dd>

(optional) Override the module version
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

</dl>

## Examples

### Build, publish, and deploy module in current folder

__Using PowerShell/Bash:__
```bash
lash deploy
```

Output:
```
LambdaSharp CLI (v0.7.0) - Deploy LambdaSharp module
Readying module for deployment tier 'Sandbox'

Reading module: Module.yml
Compiling: Demo.SlackTodo (v1.0-DEV)
=> Building function SlackCommand [netcoreapp2.1, Release]
=> Module compilation done: bin\cloudformation.json
Publishing module: Demo.SlackTodo
=> Uploading artifact: s3://lambdasharp-bucket-name/lambdasharp-bucket-name/LambdaSharp/Demo.SlackTodo/.artifacts/function_Demo.SlackTodo_SlackCommand_E0F4477DDAFDC152C8B66343657E9425.zip
=> Uploading template: s3://lambdasharp-bucket-name/lambdasharp-bucket-name/LambdaSharp/Demo.SlackTodo/.artifacts/cloudformation_Demo.SlackTodo_939992254E194760372083264D08D795.json
Resolving module reference: Demo.SlackTodo:1.0-DEV@lambdasharp-bucket-name
=> Validating module for deployment tier

Deploying stack: Sandbox-LambdaSharp-Demo-SlackTodo [Demo.SlackTodo:1.0-DEV@lambdasharp-bucket-name]
=> Stack create initiated for Sandbox-LambdaSharp-Demo-SlackTodo [CAPABILITY_IAM]
CREATE_COMPLETE    AWS::CloudFormation::Stack             Sandbox-LambdaSharp-Demo-SlackTodo
CREATE_COMPLETE    AWS::DynamoDB::Table                   TaskTable
CREATE_COMPLETE    AWS::ApiGateway::RestApi               Module::RestApi
...
CREATE_COMPLETE    AWS::Logs::SubscriptionFilter          SlackCommand::LogGroupSubscription
CREATE_COMPLETE    AWS::ApiGateway::Deployment            Module::RestApi::Deployment48BDBB7F2CFECB525DA5E89C8DF7A0E7
CREATE_COMPLETE    AWS::ApiGateway::Stage                 Module::RestApi::Stage
=> Stack create finished
Stack output values:
=> LambdaSharpTier = Sandbox
=> LambdaSharpTool = 0.7.0
=> Module = Demo.SlackTodo:1.0-DEV@lambdasharp-bucket-name
=> ModuleChecksum = 442684F838E5B6717B0EF0E74334062F
=> SlackApiPath: Slack Command URL = https://lr0iaacgoc.execute-api.us-west-2.amazonaws.com/LATEST/slack

Done (finished: 9/5/2019 1:43:03 PM; duration: 00:01:55.6433420)
```

### Deploy a published module

__Using PowerShell/Bash:__
```bash
lash deploy bin/cloudformation.json
```

Output:
```
LambdaSharp CLI (v0.7.0) - Deploy LambdaSharp module
Readying module for deployment tier 'Sandbox'

Publishing module: Demo.SlackTodo
=> Uploading artifact: s3://lambdasharp-bucket-name/lambdasharp-bucket-name/LambdaSharp/Demo.SlackTodo/.artifacts/function_Demo.SlackTodo_SlackCommand_E0F4477DDAFDC152C8B66343657E9425.zip
=> Uploading template: s3://lambdasharp-bucket-name/lambdasharp-bucket-name/LambdaSharp/Demo.SlackTodo/.artifacts/cloudformation_Demo.SlackTodo_939992254E194760372083264D08D795.json
Resolving module reference: Demo.SlackTodo:1.0-DEV@lambdasharp-bucket-name
=> Validating module for deployment tier

Deploying stack: Sandbox-LambdaSharp-Demo-SlackTodo [Demo.SlackTodo:1.0-DEV@lambdasharp-bucket-name]
=> Stack create initiated for Sandbox-LambdaSharp-Demo-SlackTodo [CAPABILITY_IAM]
CREATE_COMPLETE    AWS::CloudFormation::Stack             Sandbox-LambdaSharp-Demo-SlackTodo
CREATE_COMPLETE    AWS::DynamoDB::Table                   TaskTable
CREATE_COMPLETE    AWS::ApiGateway::RestApi               Module::RestApi
...
CREATE_COMPLETE    AWS::Logs::SubscriptionFilter          SlackCommand::LogGroupSubscription
CREATE_COMPLETE    AWS::ApiGateway::Deployment            Module::RestApi::Deployment48BDBB7F2CFECB525DA5E89C8DF7A0E7
CREATE_COMPLETE    AWS::ApiGateway::Stage                 Module::RestApi::Stage
=> Stack create finished
Stack output values:
=> LambdaSharpTier = Sandbox
=> LambdaSharpTool = 0.7.0
=> Module = Demo.SlackTodo:1.0-DEV@lambdasharp-bucket-name
=> ModuleChecksum = 442684F838E5B6717B0EF0E74334062F
=> SlackApiPath: Slack Command URL = https://lr0iaacgoc.execute-api.us-west-2.amazonaws.com/LATEST/slack

Done (finished: 9/5/2019 1:43:03 PM; duration: 00:01:55.6433420)
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
lash deploy --parameters params.yml Demo
```

### Use lookup functions in parameter file

The following functions can be used in parameter files to dynamically resolve values during the `deploy` phase.

#### !GetConfig

The `!GetConfig` function takes two arguments: the location of a JSON file and a JSON-path expression. The CLI loads the JSON file and finds the value at the JSON-path expression. The `!GetConfig` is recommended when there is a central configuration file that is used for deploying multiple modules.

##### Syntax
```yaml
!GetConfig [ json-file-path, json-path-expression ]
```

##### Parameters
<dl>

<dt><code>json-file-path</code></dt>
<dd>
The path to the JSON file, relative to the location of the parameter files.
</dd>

<dt><code>json-path-expression</code></dt>
<dd>
A JSON-path expression to locate to desired value in the JSON file. A good description of the syntax and operators can be found in <a href="https://github.com/json-path/JsonPath#jayway-jsonpath">this repository</a>.
</dd>

</dl>

#### !GetEnv

The `!GetEnv` function is similar, but reads a value from the system environment variables instead.

##### Syntax
```yaml
!GetEnv environment-variable
```

##### Parameters
<dl>

<dt><code>environment-variable</code></dt>
<dd>
The name of an environment variable.
</dd>

</dl>

#### !GetParam

The `!GetParam` function reads a value from the [AWS Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-paramstore.html) and optionally encrypts it using a KMS key.


##### Syntax
```yaml
!GetParam parameter-store-path
```
-OR-
```yaml
!GetParam [ parameter-store-path, encryption-key-id ]
```

##### Parameters
<dl>

<dt><code>parameter-store-path</code></dt>
<dd>
The path to a value in the <a href="https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-paramstore.html">AWS Parameter Store</a>.

If the value is stored as a SecureString, it is automatically decrypted when retrieved and passed as plain text, unless an <code>encryption-key-id</code> is provided.
</dd>

<dt><code>encryption-key-id</code></dt>
<dd>
The <a href="https://docs.aws.amazon.com/kms/latest/developerguide/overview.html">AWS Key Management Service</a> key ARN or alias to use for encrypting the value from the parameter store.
</dd>

</dl>

#### Examples

```yaml
ApiKey: !GetConfig [ '../global.json', Services.SomeApi.ApiKey ]
ReplyEmail: !GetParam /Company/EmailAddress
Language: !GetEnv LANG
```