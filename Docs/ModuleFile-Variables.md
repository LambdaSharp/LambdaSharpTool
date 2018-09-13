![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Variables Section

The `Variables` section is an optional mapping of key-value pairs. Variables are used in string substitutions to make it easy to change settings in a [λ# Module](ModuleFile.md).

__Topics__
* [Syntax](#syntax)
* [Examples](#examples)
* [Notes](#notes)

## Syntax

```yaml
Variables:
  VariableName: String
```

The following variables are implicitly defined and can be used in text values to dynamically compute the desired value.
* `{{Tier}}`: the name of the active deployment tier
* `{{tier}}`: the name of the active deployment tier, but in lowercase letters
* `{{Module}}`: the name of the λ# module
* `{{Version}}`: the version of the λ# module
* `{{AwsAccountId}}`: the AWS account ID
* `{{AwsRegion}}`: the AWS region
* `{{GitSha}}`: Git SHA (40 characters)

## Examples

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
    Description: My handler
    Memory: 128
    Timeout: 30
    Environment:
      GREETING: Hello {{Who}}
```

## Notes

Beware that using the `{{GitSha}}` in substitutions will cause the CloudFormation template to change with every Git revision. This means that the λ# tool will trigger a stack update every time. Even if no other values have changed!
