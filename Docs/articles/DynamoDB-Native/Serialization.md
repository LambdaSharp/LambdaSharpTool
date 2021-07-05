---
title: Serialization in DynamoDB for .NET - LambdaSharp
description: Serializing data structures with LambdaSharp.DynamoDB.Serialization
keywords: serialization, api, dynamodb, aws, amazon
---
# Serialization in DynamoDB for .NET

> TODO: `DynamoSerializationException`

> TODO: `DynamoSerializerOptions`

> TODO: `Serialize()` can return `null` if the item should not be serialized

## Overview

DynamoDB uses its own document model for serializing data structures, which looks similar to JSON. The _LambdaSharp.DynamoDB.Serialization_ library is modeled after other serialization libraries, such as _System.Text.Json.JsonSerializer_ to streamline conversions between .NET types and DynamoDB documents.

```csharp
using LambdaSharp.DynamoDB.Serialization;

var attributeValue = DynamoSerializer.Serialize(new {
    Active = true,
    Binary = Encoding.UTF8.GetBytes("Bye"),
    Name = "John Doe",
    Age = 42,
    List = new object[] {
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
    BinarySet = new[] { Encoding.UTF8.GetBytes("Good"), Encoding.UTF8.GetBytes("Day") }.ToHashSet()
});
```

### Output
```json
{
   "M": {
     "Active": { "BOOL": true },
     "Binary": { "B": "Qnll" },
     "Name": { "S": "John Doe" },
     "Age": { "N": "42" },
     "List": {
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

## Type Mappings

The following table shows how DynamoDB attribute types are mapped to .NET types. The table has two columns for .NET types. The first column describes the default .NET type used when no target type is provided. The second column describes what types are supported for (de)serializing the corresponding DynamoDB attribute values.

**Notes:**
* All .NET types must be concrete and have a parameter-less constructor.
* Only public instance properties are (de)serialized.
* Enum types are (de)serialized as strings by default.

|DynamoDB Data Type |Default .NET Type                      |Alternative .NET Type                  |
|-------------------|---------------------------------------|---------------------------------------|
|`B` (Binary)       |`byte[]`                               |N/A
|`BOOL` (Boolean)   |`bool`                                 |`bool?`
|`BS` (Binary Set)  |`HashSet<byte[]>`                      |implementation of `ISet<byte[]>`
|`L` (List)         |`List<object>`                         |implementation of `IList<T>` where `T` is a concrete type with a parameter-less constructor
|`M` (Map)          |`Dictionary<string, object>`           |any concrete type with a parameter-less constructor -OR- implementation of `IDictionary<string, T>` where `T` is a concrete type with a parameter-less constructor
|`N` (Number)       |`double`                               |`int`, `int?`, `long`, `long?`, `double?`, `decimal`, `decimal?`, `DateTimeOffset`, `DateTimeOffset?`
|`NS` (Number Set)  |`HashSet<double>`                      |implementation of `ISet<int>`, `ISet<long>`, `ISet<double>`, or `ISet<decimal>`
|`NULL` (Null)      |N/A (`null` doesn't have a .NET type)  |any class
|`S` (String)       |`string`                               |any enum type
|`SS` (String Set)  |`HashSet<string>`                      |implementation of `ISet<string>`

## JsonElem

> TODO: see `DynamoAttributeValueConverter.cs`

## String, Number, and Binary Sets

DynamoDB does not support storing empty sets. Therefore, _LambdaSharp.DynamoDB.Serialization_ will skip serializing empty instances of `ISet<string>`, `ISet<int>` etc. When a DynamoDB item is deserialized, the library detects the missing attribute and initializes the instance property with an empty instance of `HashSte<string>`, `HashSet<int>`, etc. This ensures proper round-tripping behavior for DynamoDB sets while also avoiding unnecessary null checks in code.

> TODO: `HashSet<byte[]>` is instantiated with a byte array comparer `ByteArrayEqualityComparer`


```csharp
class MyRecord {

    //--- Properties ---
    public HashSet<string> TagSet { get; set; } = new HashSet<string>();
}
```

## Serialization Attributes

> TODO: `[DynamoPropertyIgnore]`

> TODO: `[DynamoPropertyName("foo")]`

## Custom Serializers

The serialization logic can be extended by defining custom type converters either by implementing the `IDynamoAttributeConverter` interface or deriving from `ADynamoAttributeConverter` abstract base class.

> TODO: tighten up

The converter must derive from the `IDynamoAttributeConverter` class and implement the `CanConvert()`, the `ToAttributeValue()`, and one of the deserialization methods:
* `FromBool()` to convert from `AttributeValue.BOOL`
* `FromBinary()` to convert from `AttributeValue.B`
* `FromNumber()` to convert from `AttributeValue.NS`
* `FromString()` to convert from `AttributeValue.S`
* `FromList()` to convert from `AttributeValue.L`
* `FromMap()` to convert from `AttributeValue.M`
* `FromBinarySet()` to convert from `AttributeValue.BS`
* `FromNumberSet()` to convert from `AttributeValue.NS`
* `FromStringSet()` to convert from `AttributeValue.SS`



The following converter serializes `TimeSpan` from and to an `AttributeValue` instance.

```csharp
class DynamoTimeSpanConverter : ADynamoAttributeConverter {

    //--- Methods ---
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TimeSpan);

    public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options)
        => new AttributeValue {
            N = ((TimeSpan)value).TotalSeconds.ToString(CultureInfo.InvariantCulture)
        };

    public override object FromNumber(string value, Type targetType, DynamoSerializerOptions options)
        => TimeSpan.FromSeconds(double.Parse(value, CultureInfo.InvariantCulture));
}
```

The custom converter must then be added to the serialization options to become available.

```csharp
var attributes = DynamoSerializer.Serialize(record, new DynamoSerializerOptions {
    Converters = {
        new DynamoTimeSpanConverter()
    }
});
```
