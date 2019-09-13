---
title: LambdaSharp CLI Tier Command - Show Core Services Status
description: Show and update the LambdaSharp core services state for deployed modules
keywords: cli, core services, status, update, enable, disable
---
# Show/Update LambdaSharp Core Services Status for Deploy Modules

The `tier coreservices` command is used to show and updates the LambdaSharp Core Services configuration for deployed modules in a deployment tier.

## Options

<dl>

<dt><code>--enabled</code></dt>
<dd>

(optional) Enable LambdaSharp.Core services for all modules
</dd>

<dt><code>--disabled</code></dt>
<dd>

(optional) Disable LambdaSharp.Core services for all modules
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

### Show LambdaSharp Core Services status for all deployed modules

__Using PowerShell/Bash:__
```bash
lash tier coreservices --tier Sandbox
```

Output:
```
LambdaSharp CLI (v0.6.0.2) - Enable/Disable LambdaSharp.Core services for all modules in tier

Found 3 modules for deployment tier 'Sandbox'

NAME                                MODULE                                      STATUS             CORE-SERVICES
LambdaSharp-S3-IO                   LambdaSharp.S3.IO:0.6.0.2                   UPDATE_COMPLETE    ENABLED
LambdaSharp-S3-Subscriber           LambdaSharp.S3.Subscriber:0.6.0.2           UPDATE_COMPLETE    ENABLED
LambdaSharp-Demo-TwitterNotifier    Demo.TwitterNotifier:1.0-DEV    UPDATE_COMPLETE    ENABLED

Done (finished: 6/26/2019 10:16:45 PM; duration: 00:00:02.7702739)
```

### Disable LambdaSharp Core Services for all deployed modules

__Using PowerShell/Bash:__
```bash
lash tier coreservices --tier Sandbox --disable
```

Output:
```
LambdaSharp CLI (v0.6.0.2) - Enable/Disable LambdaSharp.Core services for all modules in tier

Found 3 modules for deployment tier 'Sandbox'

NAME                                MODULE                                      STATUS             CORE-SERVICES
LambdaSharp-S3-IO                   LambdaSharp.S3.IO:0.6.0.2                   UPDATE_COMPLETE    ENABLED
LambdaSharp-S3-Subscriber           LambdaSharp.S3.Subscriber:0.6.0.2           UPDATE_COMPLETE    ENABLED
LambdaSharp-Demo-TwitterNotifier    Demo.TwitterNotifier:1.0-DEV    UPDATE_COMPLETE    ENABLED

=> Stack update initiated for Sandbox-LambdaSharp-S3-IO
UPDATE_COMPLETE    AWS::CloudFormation::Stack             Sandbox-LambdaSharp-S3-IO
UPDATE_COMPLETE    AWS::Lambda::Function                  S3Writer
DELETE_COMPLETE    AWS::IAM::Policy                       ModuleRoleDeadLetterQueuePolicy
DELETE_COMPLETE    AWS::Logs::SubscriptionFilter          S3WriterLogGroupSubscription
DELETE_COMPLETE    AWS::CloudFormation::CustomResource    S3WriterRegistration
DELETE_COMPLETE    AWS::IAM::Policy                       ModuleRoleDefaultSecretKeyPolicy
DELETE_COMPLETE    AWS::CloudFormation::CustomResource    ModuleRegistration
=> Stack update finished

=> Stack update initiated for Sandbox-LambdaSharp-S3-Subscriber
UPDATE_COMPLETE    AWS::CloudFormation::Stack             Sandbox-LambdaSharp-S3-Subscriber
UPDATE_COMPLETE    AWS::Lambda::Function                  ResourceHandler
DELETE_COMPLETE    AWS::IAM::Policy                       ModuleRoleDefaultSecretKeyPolicy
DELETE_COMPLETE    AWS::Logs::SubscriptionFilter          ResourceHandlerLogGroupSubscription
DELETE_COMPLETE    AWS::IAM::Policy                       ModuleRoleDeadLetterQueuePolicy
DELETE_COMPLETE    AWS::CloudFormation::CustomResource    ResourceHandlerRegistration
DELETE_COMPLETE    AWS::CloudFormation::CustomResource    ModuleRegistration
=> Stack update finished

=> Stack update initiated for Sandbox-LambdaSharp-Demo-TwitterNotifier
UPDATE_COMPLETE    AWS::CloudFormation::Stack             Sandbox-LambdaSharp-Demo-TwitterNotifier
UPDATE_COMPLETE    AWS::CloudFormation::Stack             TwitterNotify
UPDATE_COMPLETE    AWS::Lambda::Function                  NotifyFunction
DELETE_COMPLETE    AWS::Logs::SubscriptionFilter          NotifyFunctionLogGroupSubscription
DELETE_COMPLETE    AWS::CloudFormation::CustomResource    NotifyFunctionRegistration
DELETE_COMPLETE    AWS::IAM::Policy                       ModuleRoleDeadLetterQueuePolicy
DELETE_COMPLETE    AWS::IAM::Policy                       ModuleRoleDefaultSecretKeyPolicy
DELETE_COMPLETE    AWS::CloudFormation::CustomResource    ModuleRegistration
=> Stack update finished

Found 3 modules for deployment tier 'Sandbox'

NAME                                MODULE                                      STATUS             CORE-SERVICES
LambdaSharp-S3-IO                   LambdaSharp.S3.IO:0.6.0.2                   UPDATE_COMPLETE    DISABLED
LambdaSharp-S3-Subscriber           LambdaSharp.S3.Subscriber:0.6.0.2           UPDATE_COMPLETE    DISABLED
LambdaSharp-Demo-TwitterNotifier    Demo.TwitterNotifier:1.0-DEV    UPDATE_COMPLETE    DISABLED

Done (finished: 6/26/2019 10:26:30 PM; duration: 00:02:59.1804694)
```