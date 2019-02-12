![λ#](LambdaSharp_v2_small.png)

# LambdaSharp Module Function - Alexa Source

See [Alexa sample](../Samples/AlexaSample/) for an example of how to use an Alexa skill as source.

__Topics__
* [Syntax](#syntax)
* [Properties](#properties)
* [Examples](#examples)

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
