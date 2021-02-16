---
title: Pragmas Section - Module
description: LambdaSharp module configuration pragmas
keywords: module, pragma, section, configuration, syntax, yaml, cloudformation
---
# Module Pragmas

Pragmas are used to change the default processing behavior of the LambdaSharp CLI. They are generally only required for very specific situations.

|Pragma                                 |Definition                           |
|---------------------------------------|-------------------------------------|
|`no-lambdasharp-dependencies`          |Don't reference LambdaSharp Core resources (DLQ, Logging Stream, etc.)|
|`no-module-registration`               |Don't create a module registration|
|`Overrides`                            |Override default values for built-in declarations|
|`sam-transform`                        |Add SAM template transform to CloudFormation output|

## Overrides Keys
|Key                                         |Definition                                                                          |Default                                |
|--------------------------------------------|------------------------------------------------------------------------------------|---------------------------------------|
|`Module::DeadLetterQueue`                   |Expression for determining the module dead-letter queue.                            |`!Ref LambdaSharp::DeadLetterQueue`    |
|`Module::LoggingStream`                     |Expression for determining the module logging stream.                               |`!Ref LambdaSharp::LoggingStream`      |
|`Module::LoggingStreamRole`                 |Expression for determining the module logging stream role.                          |`!Ref LambdaSharp::LoggingStreamRole`  |
|`Module::LogRetentionInDays`                |Expression for determining the number days CloudWatch Log streams are retained for. |`30`                                   |
|`Module::RestApi::CorsOrigin`               |Expression for setting the REST API CORS origin header.                             |(none)                                 |
|`Module::RestApi::StageName`                |Expression for setting the REST API stage name.                                     |`LATEST`                               |
|`Module::RestApi.EndpointConfiguration`     |Expression for setting the REST API endpoint.                                       |(none)                                 |
|`Module::RestApi.Policy`                    |Expression for setting the REST API policy.                                         |(none)                                 |
|`Module::Role.PermissionsBoundary`          |Expression for setting the PermissionsBoundary attribute on the function IAM role.  |(none)                                 |
|`Module::WebSocket::StageName`              |Expression for setting the WebSocket stage name.                                    |`LATEST`                               |
|`Module::WebSocket.ApiKeySelectionExpression`|Expression for determining the WebSocket API key.                                  |(none)                                 |
|`Module::WebSocket.RouteSelectionExpression`|Expression for determining the WebSocket route.                                     |`$request.body.action`                 |

### Examples

#### Set the route selection expression for WebSocket

The following override changes the route selection expression from `action` to `Action`.

```yaml
Pragmas:
  - Overrides:
      Module::WebSocket.RouteSelectionExpression: $request.body.Action
```

#### Make a REST API private

The following override changes the REST API definition to be private and attaches a specific policy to it. Note, the sample assumes `VpcEndpoint` parameter or variable that has details about the source VPC.

```yaml
Pragmas:
  - Overrides:
      Module::RestApi.EndpointConfiguration:
        Types:
          - PRIVATE
      Module::RestApi.Policy: !Sub |
        {
          "Version": "2012-10-17",
          "Statement": [
            {
              "Effect": "Deny",
              "Principal": "*",
              "Action": "execute-api:Invoke",
              "Resource": "arn:aws:execute-api:us-east-1:${AWS::AccountId}:*/*/*/*",
              "Condition": {
                "StringNotEquals": {
                  "aws:sourceVpc": ${VpcEndpoint}
                }
              }
            },
            {
              "Effect": "Allow",
              "Principal": "*",
              "Action": "execute-api:Invoke",
              "Resource": "arn:aws:execute-api:us-east-1:${AWS::AccountId}:*/*/*/*"
            }
          ]
        }
```
