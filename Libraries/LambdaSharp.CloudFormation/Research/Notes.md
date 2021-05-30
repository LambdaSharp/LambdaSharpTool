# Research Notes

This file contains research notes into how CloudFormation validates templates.

CloudFormation templates validated with:
```bash
aws cloudformation validate-template --template-body file://template.yml
```

> NOTES:
> Conditions: Within each condition, you can reference another condition, a parameter value, or a mapping.
> Conditionals: `Fn::If` is only supported in the metadata attribute, update policy attribute, and property values in the `Resources` section and `Outputs` sections of a template.

## Missing Checks

### `Fn::Equals` cannot be true is not detected

CloudFormation validation does not detect that `Fn::Equals` compares against a value that is not allowed.
```yaml
Parameters:
  Choice:
    Type: String
    AllowedValues:
      - First
      - Second

Conditions:

# this condition is never true
  IsThird: !Equals [ !Ref Choice, Third ]

Resources:
  FirstTopic:
    Type: AWS::SNS::Topic
    Condition: IsThird
```

```yaml
Parameters:
  Choice:
    Type: String
    AllowedPattern: "\\d\\d\\d"

Conditions:

  # this condition is never true
  IsThird: !Equals [ !Ref Choice, Hello ]

Resources:
  FirstTopic:
    Type: AWS::SNS::Topic
    Condition: IsThird
```

### Conditional circular dependencies are not detected

CloudFormation does not detect circular dependencies when obfuscated by a `Fn::If`.
```yaml
Parameters:
  Choice:
    Type: String
    AllowedValues:
      - First
      - Second

Conditions:
  IsFirst: !Equals [ !Ref Choice, First ]
  IsSecond: !Equals [ !Ref Choice, Second ]

Resources:
  FirstTopic:
    Type: AWS::SNS::Topic
    Properties:
      DisplayName: !If
        - IsFirst
        - First
        - !Ref FirstSecond

  FirstSecond:
    Type: AWS::SNS::Topic
    Properties:

      # this is the same Fn:If expression as above, which will result in a circular dependency at runtime
      DisplayName: !If
        - IsFirst
        - Second
        - !Ref FirstTopic
```

## Tricky Constructs

### It's legal to compare parameter values

```yaml
Parameters:
  First:
    Type: String

  Second:
    Type: String

Conditions:
  AreEqual: !Equals [ !Ref First, !Ref Second ]

Resources:
  FirstTopic:
    Type: AWS::SNS::Topic
    Condition: AreEqual
```

