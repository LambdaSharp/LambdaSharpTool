---
title: LambdaSharp CLI New Command - Add CloudFormation Resource to Module
description: Add a CloudFormation resource to a LambdaSharp module
keywords: cli, new, create, cloudformation, resource, module
---
# Add New Resource to Module File

The `new resource` command is used to add a resource to an existing module. The command creates a `Resource` item and generates a skeleton definition using the CloudFormation specification. Required properties are annotated with a corresponding comment. The value of the attributes are the expected type (e.g. `String`, `Json`, etc.).

## Arguments

The `new resource` command takes two arguments: the resource name and the resource type.

```bash
lash new resource MyResource AWS::SNS::Topic
```

## Options

The command has no options.

## Examples

### Create a new resource

__Using PowerShell/Bash:__
```bash
lash new resource MyResource AWS::SNS::Topic
```

Output:
```
LambdaSharp CLI (v0.6) - Create new LambdaSharp module, function, or resource
Added resource 'MyResource' [AWS::SNS::Topic]

Done (finished: 1/31/2019 9:27:27 PM; duration: 00:00:00.3311624)
```

Module:
```yaml
- Resource: MyTopic
  Description: TO-DO - update resource description
  Type: AWS::SNS::Topic
  Properties:
    # Documentation: http://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-properties-sns-topic.html
    DisplayName: String
    KmsMasterKeyId: String
    Subscription:
      - Endpoint: String # Required
        Protocol: String # Required
    TopicName: String
```

### Show partial resource type matches

__Using PowerShell/Bash:__
```bash
lash new resource MyResource sns
```

Output:
```
LambdaSharp CLI (v0.6) - Create new LambdaSharp module, function, or resource

Found partial matches for 'sns'
    AWS::SNS::Subscription
    AWS::SNS::Topic
    AWS::SNS::TopicPolicy

FAILED: 1 errors encountered
ERROR: unable to find exact match for 'SNS'

Done (finished: 4/10/2019 5:56:58 PM; duration: 00:00:00.2434111)
```