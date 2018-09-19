![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Secrets Section

The `Secrets` section, in the [λ# Module](ModuleFile.md), lists which KMS keys can be used to decrypt parameter values. The module IAM role will get the `mks:Decrypt` permission to use these keys.

__Topics__
* [Syntax](#syntax)
* [Examples](#examples)
* [Notes](#notes)

## Syntax

```yaml
Secrets:
  - Secret-Alias-or-ARN
```

## Examples

When KMS key is referenced by an alias, it is resolved on the account used when deploying the CloudFormation template.

```yaml
Secrets:
  - KeyAlias
```

When a KMS key is referenced using an ARN, it is used as is.

```yaml
Secrets:
  - arn:aws:kms:us-east-1:123456789012:key/abcdef12-3456-7890-abcd-ef1234567890
```

## Notes

AWS does not allow referencing the built-in KMS key for the [AWS Parameter Store](https://aws.amazon.com/systems-manager/features/#Parameter_Store) (i.e. `aws/ssm`).