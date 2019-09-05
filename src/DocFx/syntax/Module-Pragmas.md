# Module Pragmas

Pragmas are used to change the default processing behavior of the λ# CLI. They are generally only required for very specific situations.

|Pragma                                 |Definition                           |
|---------------------------------------|-------------------------------------|
|`no-core-version-check`                |Don't check if the λ# Core and CLI versions match|
|`no-lambdasharp-dependencies`          |Don't reference λ# Core resources (DLQ, Logging Stream, etc.)|
|`no-module-registration`               |Don't create a module registration|
|`Overrides`                            |Override default values for built-in declarations|
|`sam-transform`                        |Add SAM template transform to CloudFormation output|

### Overrides Keys
|Key                                         |Definition                                                               |Default                                |
|--------------------------------------------|-------------------------------------------------------------------------|---------------------------------------|
|`Module::DeadLetterQueue`                   |Expression for determining the module dead-letter queue.                 |`!Ref LambdaSharp::DeadLetterQueue`    |
|`Module::LoggingStream`                     |Expression for determining the module logging stream.                    |`!Ref LambdaSharp::LoggingStream`      |
|`Module::LoggingStreamRole`                 |Expression for determining the module logging stream role.               |`!Ref LambdaSharp::LoggingStreamRole`  |
|`Module::LogRetentionInDays`                |Expression for determining the number days log entries are retained for. |`30`                                   |
|`Module::WebSocket.RouteSelectionExpression`|Expression for determining the WebSocket route.                          |`"$request.body.action"`               |
