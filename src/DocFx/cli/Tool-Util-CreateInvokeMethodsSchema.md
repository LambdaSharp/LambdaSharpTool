---
title: LambdaSharp CLI Util Command - Create JSON Schema from .NET Methods
description: Generate JSON Schema via reflection of .NET methods invoked by API Gateway or WebSocket event sources
keywords: cli, json, schema, api gateway, websocket
---
# Create JSON Schema for API Gateway Methods

The `util create-invoke-methods-schema` command is used to create JSON schema definitions for methods in a given assembly. This command is invoked automatically by LambdaSharp during compilation when a module specifies target methods for `Api` or `WebSocket` routes.

## Options

<dl>

<dt><code>--default-namespace|-ns &lt;DEFAULT-NAMESPACE&gt;</code></dt>
<dd>

(optional) Default namespace for resolving class names
</dd>

<dt><code>--directory|-d &lt;DIRECTORY-PATH&gt;</code></dt>
<dd>

Directory where .NET assemblies are located
</dd>

<dt><code>--method|-m &lt;METHOD-NAME&gt;</code></dt>
<dd>

Name of a method to analyze
</dd>

<dt><code>--out|-o &lt;OUTPUT-FILE&gt;</code></dt>
<dd>

(optional) Output schema file location (default: console out)
</dd>

<dt><code>--quiet</code></dt>
<dd>

Don't show banner or execution time
</dd>

</dl>

## Examples

### Create JSON Schema for `WebSocketsSample.MessageFunction.Function::SendMessageAsync`

__Using Bash:__
```bash
lash util create-invoke-methods-schema \
    --directory MessageFunction/bin/Release/netcoreapp2.1/publish/ \
    --method MessageFunction::WebSocketsSample.MessageFunction.Function::SendMessageAsync
```

__Using PowerShell:__
```powershell
lash util create-invoke-methods-schema ^
    --assembly MessageFunction/bin/Release/netcoreapp2.1/publish/ ^
    --method MessageFunction::WebSocketsSample.MessageFunction.Function::SendMessageAsync
```

Output:
```
LambdaSharp CLI (v0.6) - Create JSON schemas for API Gateway invoke methods
Inspecting method invocation targets in MessageFunction/bin/Release/netcoreapp2.1/publish/MessageFunction.dll
... WebSocketsSample.MessageFunction.Function::SendMessageAsync: Message -> (void)
{
  "WebSocketsSample.MessageFunction.Function::SendMessageAsync": {
    "Assembly": "MessageFunction",
    "Type": "WebSocketsSample.MessageFunction.Function",
    "Method": "SendMessageAsync",
    "RequestContentType": "application/json",
    "RequestSchema": {
      "$schema": "http://json-schema.org/draft-04/schema#",
      "title": "Message",
      "type": "object",
      "required": [
        "action",
        "from",
        "text"
      ],
      "properties": {
        "action": {
          "type": "string"
        },
        "from": {
          "type": "string"
        },
        "text": {
          "type": "string"
        }
      }
    },
    "ResponseContentType": null,
    "ResponseSchema": "Void",
    "Error": null
  }
}

Done (finished: 4/9/2019 4:40:09 PM; duration: 00:00:00.5159812)
```
