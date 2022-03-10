# LambdaSharp.DynamoDB.Serialization

This package contains interfaces and classes used for serializing data structures to [AttributeValue](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_AttributeValue.html) instances used by [Amazon DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html). The _LambdaSharp.DynamoDB.Serialization_ package is modeled after other serialization libraries, such as _System.Text.Json.JsonSerializer_ to streamline conversions between .NET types and DynamoDB documents.

Visit [LambdaSharp.NET](https://lambdasharp.net/) to learn more about building serverless .NET solutions on AWS.


## Serialization

Serializing a .NET type instance is straightforward using `DynamoSerialize.Serialize<T>(T record)`.

**NOTE:** `Serialize()` will return `null` when the state of the type being serialized is not supported by DynamoDB. For example, an empty `HashSet<string>` returns `null`.

**Example:**
```csharp
using LambdaSharp.DynamoDB.Serialization;

var serialized = DynamoSerializer.Serialize(new {
    Active = true,
    Binary = Encoding.UTF8.GetBytes("Bye"),
    Name = "John Doe",
    Age = 42,
    MixedList = new object[] {
        new {
            Message = "Hello"
        },
        "World!"
    },
    Dictionary = new Dictionary<string, object> {
        ["Key"] = "Value"
    },
    StringSet = new[] { "Red", "Blue" }.ToHashSet(),
    NumberSet = new[] { 123, 456 }.ToHashSet(),
    BinarySet = new[] {
        Encoding.UTF8.GetBytes("Good"),
        Encoding.UTF8.GetBytes("Day")
    }.ToHashSet(ByteArrayEqualityComparer.Instance)
});
```

**DynamoDB Output:**
```json
{
   "M": {
     "Active": { "BOOL": true },
     "Binary": { "B": "Qnll" },
     "Name": { "S": "John Doe" },
     "Age": { "N": "42" },
     "MixedList": {
       "L": [
         {
           "M": {
             "Message": { "S": "Hello" }
           }
         },
         { "S": "World!" }
       ]
     },
     "Dictionary": {
       "M": {
         "Key": { "S": "Value" }
       }
     },
     "StringSet": {
       "SS": [ "Red", "Blue" ]
     },
     "NumberSet": {
       "NS": [ "123", "456" ]
     },
     "BinarySet": {
       "BS": [ "R29vZA==", "RGF5" ]
     }
   }
}
```

## Deserialization

Deserializing a DynamoDB document is straightforward using `DynamoSerialize.Deserialize<T>(Dictionary<string, AttributeValue> document)`.

**NOTE:** `Deserialize()` will return `null` when deserialized the `NULL` attribute value.

**NOTE:** `Deserialize()` may throw `DynamoSerializationException` when an issue occurs during deserialization.


## Serializer Options

The behavior of `DynamoSerialize.Serialize<T>(...)` and `DynamoSerialize.Deserialize<T>(...)` can be modified by supplying an instance of `DynamoSerializerOptions` as a second parameter.

**NOTE:** It is recommended to create and share a single instance `DynamoSerializerOptions`.

### Properties

The `DynamoSerializerOptions` has the following properties.

<dl>

<dt><code>Converters</code></dt>
<dd>

The <code>Converters</code> property lists additional custom converters to use when (de)serializing values. Custom converters take precedence over default converters. Default converters can be disabled entirely by setting the <code>UseDefaultConverters</code> property to <code>false</code>.

<em>Type:</em> <code>List&lt;IDynamoAttributeConverter&gt;</code>

<em>Default:</em> empty list

</dd>


<dt><code>IgnoreNullValues</code></dt>
<dd>

The <code>IgnoreNullValues</code> property controls if <code>null</code> values are serialized as DynamoDB NULL attribute values or skipped.

<em>Type:</em> <code>bool</code>

<em>Default:</em> <code>true</code>

</dd>


<dt><code>UseDefaultConverters</code></dt>
<dd>

The <code>UseDefaultConverters</code> property controls if the default DynamoDB converters are enabled.

<em>Type:</em> <code>bool</code>

<em>Default:</em> <code>true</code>

The default converters are:
* `DynamoBoolConverter`
* `DynamoIntConverter`
* `DynamoLongConverter`
* `DynamoDoubleConverter`
* `DynamoDateTimeOffsetConverter`
* `DynamoFloatConverter`
* `DynamoDecimalConverter`
* `DynamoStringConverter`
* `DynamoEnumConverter`
* `DynamoByteArrayConverter`
* `DynamoISetByteArrayConverter`
* `DynamoISetStringConverter`
* `DynamoISetIntConverter`
* `DynamoISetLongConverter`
* `DynamoISetDoubleConverter`
* `DynamoISetDecimalConverter`
* `DynamoIDictionaryConverter`
* `DynamoListConverter`
* `DynamoJsonElementConverter`
* `DynamoObjectConverter`

</dd>

</dl>


## Serialization Attributes for DynamoDB

By default, all public properties of a class are (de)serialized by `DynamoSerializer` using the property name. However, this behavior can modified by annotating the properties with the following attributes.

<dl>

<dt><code>DynamoPropertyIgnore</code></dt>
<dd>

The <code>DynamoPropertyIgnore</code> attribute causes <code>DynamoSerializer</code> to ignore a property on a class.

</dd>


<dt><code>DynamoPropertyName(string Name)</code></dt>
<dd>

The <code>DynamoPropertyName</code> attribute changes the name used by <code>DynamoSerializer</code> when serializing the property.

</dd>

</dl>

```csharp
class MyRecord {

    //--- Properties ---

    [DynamoPropertyIgnore]
    public string IgnoreMe { get; set; }

    [DynamoPropertyName("NewName")]
    public string RenameMe { get; set; }
}
```

## License

> Copyright (c) 2018-2022 LambdaSharp (λ#)
>
> Licensed under the Apache License, Version 2.0 (the "License");
> you may not use this file except in compliance with the License.
> You may obtain a copy of the License at
>
> http://www.apache.org/licenses/LICENSE-2.0
>
> Unless required by applicable law or agreed to in writing, software
> distributed under the License is distributed on an "AS IS" BASIS,
> WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
> See the License for the specific language governing permissions and
> limitations under the License.
