![λ#](../../src/DocFx/images/LambdaSharpLogo.png)

# LambdaSharp Function in VPC

Before you begin, make sure to [setup your LambdaSharp CLI](https://lambdasharp.net/articles/Setup.html).

## Module Definition

Functions, like resources, have a `Properties` section that can be used to fine tune the Lambda settings, such as putting the Lambda function in a VPC.

For additional details about what can be set in the `Properties` section see the [`AWS::Lambda::Function` type documentation](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-resource-lambda-function.html).

```yaml
Module: Sample.VpcFunction
Description: A sample module using function in VPC
Items:

  - Parameter: SecurityGroupIds
    Type: CommaDelimitedString

  - Parameter: SubnetIds
    Type: CommaDelimitedString

  - Function: MyFunction
    Memory: 128
    Timeout: 30
    Properties:
      VpcConfig:
        SecurityGroupIds: !Ref SecurityGroupIds
        SubnetIds: !Ref SubnetIds
```
