![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module - Output Definition

Output values are converted into [CloudFormation export](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/using-cfn-stack-exports.html) values for top-level stacks. For nested stacks, output values are converted into CloudFormation stack outputs. This behavior prevents other modules from taking dependencies on nested stacks.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

## Syntax

```yaml
Output: String
Description: String
Value: String
```

## Properties

<dl>

<dt><code>Output</code></dt>
<dd>
The <code>Output</code> attribute specifies the name of the module's output variable. If the <code>Output</code> value matches an input or variable name, the <code>Description<code> and <code>Value</code> attributes are copied from the matching input or variable when omitted.

<i>Required</i>: Yes

<i>Type</i>: String
</dd>

<dt><code>Description</code></dt>
<dd>
The <code>Description</code> attribute specifies the description of the module's output variable.

<i>Required</i>: No. The <code>Description</code> attribute can be omitted if the <code>Output</code> attribute matches an input or variable name. In that case, the description of the input/variable is used as description for the output value.

<i>Type</i>: String
</dd>

<dt><code>Value</code></dt>
<dd>
The <code>Value</code> attribute specifies either a literal output value or an expression that evaluates to the desired output value.

<i>Required</i>: Conditional. The <code>Value</code> attribute can be omitted if the <code>Output</code> attribute matches an input or variable name. In that case, the value of the input/variable is used as output value.

<i>Type</i>: String Expression
</dd>

</dl>

## Examples

### Output value is a literal value

```yaml
- Output: FixedValue
  Description:
  Value: Hello World!
```

### Output value is the attribute of a resource

```yaml
- Var: MyQueue
  Resource:
    Type: AWS::SQS::Queue
    Allow: Send

# ...

- Output: QueueArn
  Description:
  Value: !GetAtt MyQueue.Arn
```

### Output value is a variable

```yaml
- Var: MyQueue
  Description: My SQS queue
  Resource:
    Type: AWS::SQS::Queue
    Allow: Send

# ...
- Output: MyQueue
```
