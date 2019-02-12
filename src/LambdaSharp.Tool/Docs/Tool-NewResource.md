![λ#](../../../Docs/LambdaSharp_v2_small.png)

# LambdaSharp CLI - New Resource Command

The `new resource` command is used to add a resource to an existing module. The command creates a `Resource` item and generates a skeleton definition using the CloudFormation specification. Required properties are annotated with a corresponding comment. The value of the attributes are the expected type (e.g. `String`, `Json`, etc.).

## Arguments

The `new resource` command takes two arguments: the resource name and the resource type.

```bash
lash new resource MyResource AWS::SNS::Topic
```

## Options

The command has no options.

## Examples

### Create a new module in the current folder

__Using PowerShell/Bash:__
```bash
lash new resource MyResource AWS::SNS::Topic
```

Output:
```
LambdaSharp CLI (v0.5) - Create new LambdaSharp module, function, or resource
Added resource 'MyResource' [AWS::SNS::Topic]

Done (finished: 1/31/2019 9:27:27 PM; duration: 00:00:00.3311624)
```

Module:
```yaml
- Resource: MyResource
  Description: TODO - update resource description
  Scope: [ ]
  Type: AWS::SNS::Topic
  Allow: [ ]
  Properties:
    DisplayName: String
    KmsMasterKeyId: String
    Subscription:
      - Endpoint: String # Required
        Protocol: String # Required
    TopicName: String
```