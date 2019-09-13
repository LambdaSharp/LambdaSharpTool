---
title: Alexa Event Source Declaration - Function
description: LambdaSharp YAML syntax for Amazon Alexa event source
keywords: amazon, alexa, event source, declaration, lambda, syntax, yaml, cloudformation
---
# Alexa Source

See [Alexa sample](https://github.com/LambdaSharp/LambdaSharpTool/tree/master/Samples/AlexaSample/) for an example of how to use an Alexa skill as source.

## Syntax

```yaml
Alexa: String|Expression
```

## Properties

<dl>

<dt><code>Alexa</code></dt>
<dd>

The <code>Alexa</code> attribute can either specify an Alexa Skill ID or the wildcard value (<code>"*"</code>) to allow any Alexa skill to invoke it.

<i>Required</i>: Yes

<i>Type</i>: String or Expression
</dd>

</dl>

## Examples

Allow any Alexa skill to invoke this function.

```yaml
Function: MyFunction
Memory: 128
Timeout: 15
Sources:
    - Alexa: "*"
```

Use Alexa Skill ID specified by a module variable.
```yaml
Function: MyFunction
Memory: 128
Timeout: 15
Sources:
    - Alexa: !Ref MyAlexaSkillID
```
