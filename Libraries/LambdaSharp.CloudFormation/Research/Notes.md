# Research Notes

This file contains research notes into how CloudFormation validates templates.

CloudFormation templates validated with:
```bash
aws cloudformation validate-template --template-body file://template.yml
```

> NOTES:
> Conditions: Within each condition, you can reference another condition, a parameter value, or a mapping.
> Conditionals: `Fn::If` is only supported in the metadata attribute, update policy attribute, and property values in the `Resources` section and `Outputs` sections of a template.
> Mappings: ensure that level-2 keys are consistent for all level-1 choices (seems wrong that it wouldn't be!)
>   * consider requiring `AllowedValues` property when parameter is used to find keys in a map

## Missing Checks

### `Fn::Equals` cannot be true is not detected

Validation does not detect that `Fn::Equals` compares against a value that is not part of the allowed values constraint.
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

Validation does not detect that `Fn::Equals` compares against a value that does not match the allowed pattern.
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

Validate does not detect that `Fn::Equals` compares against a mapping value that does not exist.
```yaml
Parameters:
  First:
    Type: String
  Second:
    Type: String

Mappings:
  MyMapping:
    FirstValue1:
      SecondValue1: 1-1
      SecondValue2: 1-2
    FirstValue2:
      SecondValue1: 2-1
      SecondValue2: 2-2
    FirstValue3:
      SecondValue1: 3-1
      SecondValue2: 3-2

Conditions:
  MyCondition: !Equals [ !FindInMap [ MyMapping, !Ref First, !Ref Second ], Hello ]

Resources:
  FirstTopic:
    Type: AWS::SNS::Topic
    Condition: MyCondition
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

### It's legal to compare mappings values

```yaml
Parameters:
  First:
    Type: String
  Second:
    Type: String

Mappings:
  MyMapping:
    FirstValue1:
      SecondValue1: 1-1
      SecondValue2: 1-2
    FirstValue2:
      SecondValue1: 2-1
      SecondValue2: 2-2
    FirstValue3:
      SecondValue1: 3-1
      SecondValue2: 3-2

Conditions:
  MyCondition: !Equals [ !FindInMap [ MyMapping, !Ref First, !Ref Second ], !FindInMap [ MyMapping, !Ref Second, !Ref First ] ]

Resources:
  FirstTopic:
    Type: AWS::SNS::Topic
    Condition: MyCondition
```