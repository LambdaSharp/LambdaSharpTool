![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module File

The λ# module file defines the parameters and functions of a module. Parameters can be either values or AWS resources. Functions are .NET Core projects that are wired up to various invocation sources defined in the module file. Parameters, resources, and their access permissions are shared across all functions that are part of the same module. The λ# tool generates the CloudFormation template, compiles .NET Core projects, uploads all assets, and automatically creates/updates the CloudFormation stack.

__Table of Contents__
1. [General](#general)
1. [Variables](#variables)
1. [Secrets](#secrets)
1. [Parameters](#parameters)
1. [Functions](#functions)

## Module

```yaml
Name: String
Version: String
Description: String
Variables:
  VariableDefinitions
Secrets:
  - String
Parameters:
  - ParameterDefinition
Functions:
  - FunctionDefinition
```

<dl>
<dt><tt>Name</tt></dt>
<dd>
The <tt>Name</tt> attribute defines the name of the λ# module. It is used as prefix for CloudFormation resources, as well as to access to the parameter store. The module name can be accessed as a variable in string substitutions using the <tt>{{Module}}</tt> notation.

<i>Required:</i> Yes

<i>Type:</i> String
</dd>

<dt><tt>Version</tt></dt>
<dd>
The <tt>Version</tt> attribute defines the version of the λ# module. It is exported to the parameter store to track the version of deployed modules. The module version can be accessed as a variable in string substitutions using the <tt>{{Version}}</tt> notation.

The format of the version must be <tt>Major.Minor[.Build[.Revision]]</tt>. Components in square brackets (<tt>[]</tt>) are optional and can be omitted.

<i>Required:</i> No

<i>Type:</i> String
</dd>

<dt><tt>Description</tt></dt>
<dd>
The <tt>Description</tt> attribute value is shown with the CloudFormation stack.

<i>Required:</i> No

<i>Type:</i> String
</dd>

<dt><tt>Variables</tt></dt>
<dd>
The <tt>Variables</tt> sections is an optional dictionary of key-value pairs. Variables are used in string substitutions to make it easy to change settings in the module file.

<i>Required:</i> No

<i>Type:</i> [Variable Definitions](#variables)
</dd>

<dt><tt>Secrets</tt></dt>
<dd>
The <tt>Secrets</tt> section lists which KMS keys can be used to decrypt parameter values. The module IAM role will get permission to use these keys (i.e. `mks:Decrypt`).

<i>Required:</i> No

<i>Type:</i> List of String (see [Secrets](#secrets))
</dd>

<dt><tt>Parameters</tt></dt>
<dd>
The <tt>Parameters</tt> section contains the parameter values and resources for the module. In addition, these values can be published to the AWS Systems Manager Parameter Store for easy access by other modules and sysadmins.

<i>Required:</i> No

<i>Type:</i> List of [Parameter](#parameters)
</dd>

<dt><tt>Functions</tt></dt>
<dd>
The <tt>Functions</tt> section contains the lambda functions that are part of the module. All functions receive the same IAM role and have equal access to all parameters.

<i>Required:</i> No

<i>Type:</i> List of [Function](#functions)
</dd>
</dl>

## Variables

The `Variables` sections is an optional mapping of key-value pairs. Variables are used in string substitutions to make it easy to change settings in the module file.

The following variables are implicitly defined and can be used in text values to dynamically compute the correct value.
* `{{Tier}}`: the name of the active deployment tier
* `{{tier}}`: the name of the active deployment tier, but in lowercase letters
* `{{Module}}`: the name of the λ# module
* `{{Version}}`: the version of the λ# module
* `{{AwsAccountId}}`: the AWS account ID
* `{{AwsRegion}}`: the AWS region
* `{{GitSha}}`: Git SHA (40 characters)

**NOTE:** Beware that using the `{{GitSha}}` in substitutions will cause the CloudFormation template to change with every Git revision. This means that the λ# tool will trigger a stack update every time. Even if no other values have changed!

Variables are used by parameters and substituted during the build phase.

```yaml
Variables:
  Who: world

Parameters:
  - Name: MyWelcomeMessage
    Value: Hello {{Who}}!
```

Variables can also be used in other variables to create compound values. The order of definitions for variables is not important. However, beware to avoid cyclic dependencies, otherwise the λ# tool will be unable to resolve the variable value.

```yaml
Variables:
  Who: world
  Greeting: Hello {{Who}}

Parameters:
  - Name: MyWelcomeMessage
    Value: "{{Greeting}}"
```

Variables can be used in any location where a value is expected.

```yaml
Variables:
  Who: world

Functions:

  - Name: MyWelcomeFunction
    Memory: 128
    Timeout: 30
    Environment:
      GREETING: Hello {{Who}}
```

## Secrets

The `Secrets` section lists which KMS keys can be used to decrypt parameter values. The module IAM role will get the `mks:Decrypt` permission to use these keys.

```yaml
Secrets:

  # When KMS key is referenced by an alias, it is resolved on the
  # account used for deploying the CloudFormation template.
  - KeyAlias

  # When a KMS key is referenced using an ARN, it is used as is.
  - arn:aws:kms:us-east-1:123456789012:key/abcdef12-3456-7890-abcd-ef1234567890
```

## Parameters

Parameters can be defined inline in plaintext, as secrets, imported from the [AWS Systems Manager Parameter Store](https://aws.amazon.com/systems-manager/features/#Parameter_Store), or generated dynamically. In addition, parameters can be associated to resources, which will grant the module IAM role the requested permissions. Finally, parameters can also be exported to the Parameter Store where they can be read by other applications.


Parameters must have a `Name` and MAY have a `Description`. The name must start with a letter and followed only by letters or digits. Punctuation marks are not allowed. All names are case-sensitive.

The computed values are stored in the `parameters.json` file that is included with every Lambda function deployment, so that they can retrieved during execution.

```yaml
Name: String
Description: String
Value: String
Values:
  - String
Secret: String
Import: String
Package:
  PackageDefinition
Export: String
Resource:
  ResourceDefinition
```

<dl>
<dt><tt>Name</tt></dt>
<dd>
The <tt>Name</tt> attribute specifies the parameter name used by Lambda functions to retrieve the parameter value.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><tt>Description</tt></dt>
<dd>
The <tt>Description</tt> attribute specifies the parameter description used by the AWS Systems Manager Parameter Store for exported parameter values.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><tt>Value</tt></dt>
<dd>
The <tt>Value</tt> attribute specifies the plaintext value for the parameter. When used in conjunction with the <tt>Resource</tt> section, the <tt>Value</tt> attribute must begin with <tt>arn:</tt> or be a global wildcard (i.e. <tt>*</tt>).

<i>Required</i>: No. At most one <tt>Value</tt>, <tt>Values</tt>, <tt>Secret</tt>, <tt>Import</tt>, or <tt>Package</tt> can be specified at a time.

<i>Type</i>: String
</dd>

<dt><tt>Values</tt></dt>
<dd>
The <tt>Values</tt> section is a list of plaintext values that are concatenated into a single parameter value using commas (<tt>,</tt>).

The <tt>Values</tt> section cannot be used in conjunction with the <tt>Resource</tt> section.

<i>Required</i>: No. At most one <tt>Value</tt>, <tt>Values</tt>, <tt>Secret</tt>, <tt>Import</tt>, or <tt>Package</tt> can be specified at a time.

<i>Type</i>: List of String
</dd>
</dl>

<dt><tt>Secret</tt></dt>
<dd>
The <tt>Secret</tt> attribute specifies an encrypted value that is decrypted at runtime by the Lambda function. Note that the required decryption key must be specified in the <tt>Secrets</tt> section to grant <tt>kms:Decrypt</tt> to module IAM role.

The <tt>Secret</tt> attribute cannot be used in conjunction with a <tt>Resource</tt> section or <tt>Export</tt> attribute.

<i>Required</i>: No. At most one <tt>Value</tt>, <tt>Values</tt>, <tt>Secret</tt>, <tt>Import</tt>, or <tt>Package</tt> can be specified at a time.

<i>Type</i>: String
</dd>

<dt><tt>Import</tt></dt>
<dd>
The <tt>Import</tt> attribute specifies a path to the AWS Systems Manager Parameter Store. At build time, the λ# tool imports the value and stores it in the <tt>parameters.json</tt> file. If the value starts with <tt>/</tt>, it will be used as an absolute key path. Otherwise, it will be prefixed with <tt>/{{Tier}}/</tt> to create an import path specific to the deployment tier.

<i>Required</i>: No. At most one <tt>Value</tt>, <tt>Values</tt>, <tt>Secret</tt>, or <tt>Import</tt> can be specified at a time.

<i>Type</i>: String
</dd>

<dt><tt>Package</tt></dt>
<dd>
The <tt>Package</tt> section specifies local files with a destination S3 bucket and an optional destination key prefix. At build time, the λ# tool creates a package of the local files and automatically copies them to the destination S3 bucket during deployment.

<i>Required</i>: No. At most one <tt>Value</tt>, <tt>Values</tt>, <tt>Secret</tt>, <tt>Import</tt>, or <tt>Package</tt> can be specified at a time.

<i>Type</i>: [Package Definition](#package)
</dd>

<dt><tt>Export</tt></dt>
<dd>
The <tt>Export</tt> attribute specifies a path to the AWS Systems Manager Parameter Store. When the CloudFormation stack is deployed, the parameter value is published to the parameter store at the export path. If the export path starts with <tt>/</tt>, it will be used as an absolute path. Otherwise the export path is prefixed with <tt>/{{Tier}}/{{Module}}/</tt> to create an export path specific to the deployment tier.

The <tt>Export</tt> attribute cannot be used in conjunction with the <tt>Secret</tt> attribute.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><tt>Resource</tt></dt>
<dd>
The parameter value corresponds to one or more AWS resources. A new, managed AWS resource is created when the <tt>Resource</tt> section is used without the <tt>Value</tt> attribute. Otherwise, one or more existing AWS resources are referenced. The resulting resource value (ARN, Queue URL, etc.) becomes the parameter value after initialization and can be retrieve during function initialization.

The <tt>Resource</tt> section cannot be used in conjunction with the <tt>Values</tt> section or <tt>Secret</tt> attribute.

<i>Required</i>: No

<i>Type</i>: [Resource Definition](#resource)
</dd>
</dl>

### Package

The `Package` section specifies local files with a destination S3 bucket and an optional destination key prefix. At build time, the λ# tool creates a package of the local files and automatically copies them to the destination S3 bucket during deployment.

```yaml
Files: String
Bucket: String
Prefix: String
```

<dl>
<dt><tt>Files</tt></dt>
<dd>
The <tt>Files</tt> attribute specifies a path to a local folder. The path can optionally have a wildcard suffix (e.g. <tt>*.json</tt>). If the wildcard suffix is omitted, all files and sub-folders are included, recursively. Otherwise, only the top folder and files matching the wildcard are included.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><tt>Bucket</tt></dt>
<dd>
The <tt>Bucket</tt> attribute specifies the name of a resource parameter of type <tt>AWS::S3::Bucket</tt> that is the destination for the files.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><tt>Prefix</tt></dt>
<dd>
The <tt>Prefix</tt> attribute specifies a key prefix that is prepended to all copied files.

<i>Required</i>: No

<i>Type</i>: String
</dd>
</dl>

### Resource

The presence of the `Resource` section indicates that the parameter corresponds to an AWS resource. Similarly to regular parameter values, the resource values (e.g. ARN, Queue URL, etc.) are passed to the Lambda functions, so that they can retrieved during function initialization.

Access permissions can be specified using the IAM `Allow` notation or the λ# shorthand notation for supported resources. See the [λ# Shorthand by Resource Type](../src/MindTouch.LambdaSharp.Tool/Resources/IAM-Mappings.yml) YAML file for up-to-date support.

```yaml
Type: String
Allow: AllowDefinition
Properties:
  ResourceProperties
```
<dl>
<dt><tt>Type</tt></dt>
<dd>
The <tt>Type</tt> attribute identifies the AWS resource type that is being declared. For example, <tt>AWS::SNS::Topic</tt> declares an SNS topic. For a list of all resource types, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><tt>Allow</tt></dt>
<dd>
The <tt>Allow</tt> attribute can either a comma-separated, single string value or a list of string values. String values that contain a colon (<tt>:</tt>) are interpreted as IAM permission and used as is (e.g. <tt>dynamodb:GetItem</tt>, <tt>s3:GetObject*</tt>, etc.). Otherwise, the value is interpreted as a λ# shorthand (see <a href="../src/MindTouch.LambdaSharp.Tool/Resources/IAM-Mappings.yml">λ# Shorthand by Resource Type</a>). Both notations can be used simultaneously within a single <tt>Allow</tt> section. Duplicate IAM permissions, after λ# shorthand resolution, are removed.

<i>Required</i>: No

<i>Type</i>: Either String or List of String
</dd>

<dt><tt>Properties</tt></dt>
<dd>
The <tt>Properties</tt> section specifies additional options that can be specified for a managed resource. The <tt>Properties</tt> section cannot be specified for referenced resources. For a list of all additional options, see <a href="https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html">AWS Resource Types Reference</a>.

<i>Required</i>: No

<i>Type</i>: Map
</dd>
</dl>

## Functions

The `Functions` section may contain zero or more function definitions. Each definition corresponds to a .NET Core project that is compiled and deployed. The published Lambda functions are prefixed with `{{Tier}}-{{Module}}.` to uniquely distinguish them from other published functions.

```yaml
Name: String
Description: String
Memory: Int
Timeout: Int
Project: String
Handler: String
Runtime: String
ReservedConcurrency: Int
Environment:
  String: String
Sources:
  - SourceDefinition
```

<dl>
<dt><tt>Name</tt></dt>
<dd>
The <tt>Name</tt> attribute specifies the function name used to publish the Lambda function.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><tt>Description</tt></dt>
<dd>
The <tt>Description</tt> attribute value is used by the AWS Lambda function.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><tt>Memory</tt></dt>
<dd>
The <tt>Memory</tt> attribute specifies the memory limit for the lambda function. The value must be in the range of 128 MB up to 3008 MB, in 64 MB increments.
</pre>

<i>Required</i>: Yes

<i>Type</i>: Int
</dd>

<dt><tt>Timeout</tt></dt>
<dd>
The <tt>Timeout</tt> attribute specifies the execution time limit in seconds. The maximum value is 300 seconds.

<i>Required</i>: Yes

<i>Type</i>: Int
</dd>

<dt><tt>Project</tt></dt>
<dd>
The <tt>Project</tt> attribute specifies the relative path of the .NET Core project file location for the lambda function.

<i>Required</i>: Conditional. By default, the .NET Core project file is expected to be located in a sub-folder of the module file, following this naming convention: <code>{{Module}}.{{FunctionName}}/{{Module}}.{{FunctionName}}.csproj</code>. If that is not the case, then the <tt>Project</tt> attribute must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><tt>Handler</tt></dt>
<dd>
The <tt>Handler</tt> attribute specifies the fully qualified .NET Core method reference to the Lambda function handler.

<i>Required</i>: Conditional. By default, the .NET Core method reference is expected to be <code>{{Module}}.{{FunctionName}}::{{Namespace}}.Function::FunctionHandlerAsync</code> where <tt>{{Namespace}}</tt> is determined by inspecting the <tt>&lt;RootNamespace&gt;</tt> element of the .NET Core project file. If the Lambda function handler is not called <tt>FunctionHandlerAsync</tt>, or the class implemented it is not called <tt>Function</tt>, or the <tt>&lt;RootNamespace&gt;</tt> is not specified in the .NET Core project file, or the .NET Core assembly name is not <tt>{{Module}}.{{FunctionName}}</tt>, the the <tt>Handler</tt> attribute must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><tt>Runtime</tt></dt>
<dd>
The <tt>Runtime</tt> attribute specifies the Lambda runtime to use to run the function.

<i>Required</i>: Conditional. By default, the runtime is determined by inspecting the .NET Core project file. If the runtime cannot be determined automatically, then it must be specified. Otherwise, it can be omitted.

<i>Type</i>: String
</dd>

<dt><tt>ReservedConcurrency</tt></dt>
<dd>
The <tt>ReservedConcurrency</tt> attribute specifies the number of Lambda invocation slots to reserve for this Lambda function. The invocation slots are drawn from a global pool and once allocated by a function, cannot be used by any other Lambda function on the AWS account. At the same time, <tt>ReservedConcurrency</tt> value also specifies the maximum number of concurrent Lambda executions for the function. When omitted, the Lambda function has no limit in number of invocations and also does not prevent other Lambda functions from being invoked.

<i>Required</i>: No

<i>Type</i>: Int
</dd>

<dt><tt>Environment</tt></dt>
<dd>
The <tt>Environment</tt> sections contains key-value pairs that correspond to custom <a href="https://docs.aws.amazon.com/lambda/latest/dg/env_variables.html">Lambda environment variables</a> which can be retrieved by the Lambda function during initialization.

<i>Required</i>: No

<i>Type</i>: Map of key-value pairs
</dd>

<dt><tt>Sources</tt></dt>
<dd>
The <tt>Sources</tt> section contains zero or more source definitions the Lambda function expects to be invoked by. Each source automatically grants the Lambda invocation permission to the invoking service/resource.

<i>Required</i>: No

<i>Type</i>: List of [Source Definition](#sources)
</dd>
</dl>

### Sources

Sources invoke Lambda functions based on requests or events. The type of payload received by the invocation varies by source type. See the [λ# Samples](../Samples/) for how the handle the various sources.

#### Alexa Source

See [Alexa sample](../Samples/AlexaSample/) for an example of how to use an Alexa skill as source.

```yaml
Alexa: String
```

<dl>
<dt><tt>Alexa</tt></dt>
<dd>
The <tt>Alexa</tt> attribute can either specify an Alexa Skill ID or the wildcard value (`"*'`) to allow any Alexa skill to invoke it.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>
</dl>

#### API Gateway Source

The λ# tool uses the <a href="https://docs.aws.amazon.com/apigateway/latest/developerguide/set-up-lambda-proxy-integrations.html#api-gateway-create-api-as-simple-proxy">API Gateway Lambda Proxy Integration</a> to invoke Lambda functions from API Gateway. See [API Gateway sample](../Samples/ApiSample/) for an example of how to use the API Gateway source.


```yaml
Api: String
```

<dl>
<dt><tt>Api</tt></dt>
<dd>
The <tt>Api</tt> attribute specifies the HTTP method and resource path that is mapped to the Lambda function. The notation is <nobr><code>METHOD /resource/subresource/{param}</code></nobr>. The API Gateway instance, the API Gateway resources, and the API Gateway methods are automatically created for the module when an API Gateway source is used.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>
</dl>

##### Examples

```yaml
Sources:
  - Api: POST /items
  - Api: GET /items/{id}
  - Api: DELETE /items/{id}
```

#### CloudWatch Schedule Event Source

See [CloudWatch Schedule Event sample](../Samples/ScheduleSample/) for an example of how to use the CloudWatch Schedule Event source.

```yaml
Schedule: String
Name: String
```

<dl>
<dt><tt>Schedule</tt></dt>
<dd>
The <tt>Schedule</tt> attribute specifies a <tt>cron</tt> or <tt>rate</tt> expression that defines a schedule for regularly invoking the Lambda function. See <a href="https://docs.aws.amazon.com/lambda/latest/dg/tutorial-scheduled-events-schedule-expressions.html">Schedule Expressions Using Rate or Cron</a> for more information.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><tt>Name</tt></dt>
<dd>
The <tt>Name</tt> attribute specifies a name for this CloudWatch Schedule Event to distinguish it from other CloudWatch Schedule Event sources.

<i>Required</i>: No

<i>Type</i>: String
</dd>
</dl>

##### Examples

```yaml
Sources:
  - Schedule: cron(0 12 * * ? *)
  - Schedule: rate(1 minute)
    Name: MyEvent
```

#### S3 Bucket Source

See [S3 Bucket sample](../Samples/S3Sample/) for an example of how to use the S3 Bucket source.

```yaml
S3: String
Events: [ String ]
Prefix: String
Suffix: String
```

<dl>
<dt><tt>S3</tt></dt>
<dd>
The <tt>S3</tt> attribute specifies the name of a resource parameter of type <tt>AWS::S3::Bucket</tt> that is the origin of the events.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><tt>Events</tt></dt>
<dd>
The <tt>Events</tt> section specifies the S3 events that trigger an invocation of the Lambda function. By default, the Lambda function only reacts to <code>s3:ObjectCreated:*</code> events. See <a href="https://docs.aws.amazon.com/AmazonS3/latest/dev/NotificationHowTo.html#notification-how-to-event-types-and-destinations">S3 Event Notification Types and Destinations</a> for a complete list of S3 events.

<i>Required</i>: No

<i>Type</i>: List of String
</dd>

<dt><tt>Prefix</tt></dt>
<dd>
The <tt>Prefix</tt> attribute specifies a filter to limit invocations to object key names that begin with the attribute value.

<i>Required</i>: No

<i>Type</i>: String
</dd>

<dt><tt>Suffix</tt></dt>
<dd>
The <tt>Suffix</tt> attribute specifies a filter to limit invocations to object key names that end with the attribute value.

<i>Required</i>: No

<i>Type</i>: String
</dd>
</dl>

##### Examples

```yaml
Sources:

  # listen to `s3:ObjectCreated:*` on the bucket
  - S3: MyFirstBucket

  # listen to custom events on specific S3 keys
  - S3: MySecondBucket
    Events:
      - "s3:ObjectCreated:*"
      - "s3:ObjectRemoved:*"
    Prefix: images/
    Suffix: .png
```

#### Slack Command Source

For Slack commands, the λ# tool deploys an asynchronous API Gateway endpoint that avoids timeout errors due to slow Lambda functions. See [Slack Command sample](../Samples/SlackCommandSample/) for an example of how to use the Slack Command source.

```yaml
SlackCommand: String
```

<dl>
<dt><tt>SlackCommand</tt></dt>
<dd>
The <tt>SlackCommand</tt> attribute specifies the resource path that is mapped to the Lambda function. The notation is <nobr><code>/resource/subresource</code></nobr>. Similarly to the API Gateway source, the API Gateway instance, the API Gateway resources, and the API Gateway methods are automatically created for the module when a Slack Command source is used.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>
</dl>

##### Examples

```yaml
Sources:
  - SlackCommand: /slack
```

#### SNS Topic Source

See [SNS Topic sample](../Samples/SnsSample/) for an example of how to use the SNS Topic source.

<pre>
      # TOPIC (optional)
      # The parameter name of a SNS resource. The resource requires
      # the `sns:Subscribe` permission.
</pre>

```yaml
Topic: String
```

<dl>
<dt><tt>Topic</tt></dt>
<dd>
The <tt>Topic</tt> attribute specifies the name of a resource parameter of type <tt>AWS::SNS::Topic</tt> that the Lambda function subscribes to.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>
</dl>

##### Examples

```yaml
Sources:
  - Topic: SnsTopic
```

#### SQS Queue Source

See [SQS Queue sample](../Samples/SqsSample/) for an example of how to use the SQS Queue source.

```yaml
Sqs: String
BatchSize: int
```

<dl>
<dt><tt>Sqs</tt></dt>
<dd>
The <tt>Sqs</tt> attribute specifies the name of a resource parameter of type <tt>AWS::SQS::Queue</tt> that the Lambda function fetches messages from.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><tt>BatchSize</tt></dt>
<dd>
The <tt>BatchSize</tt> attribute specifies the maximum number of messages to fetch from the SQS queue. The value must be in the range from 1 to 10.

<i>Required</i>: No

<i>Type</i>: Int
</dd>
</dl>
