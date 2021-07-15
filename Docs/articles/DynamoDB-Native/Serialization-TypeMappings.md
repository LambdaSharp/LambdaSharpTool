---
title: .NET Type Mappings for DynamoDB - LambdaSharp
description: Serializing data structures with LambdaSharp.DynamoDB.Serialization
keywords: serialization, api, dynamodb, aws, amazon
---
# .NET Type Mappings for DynamoDB

The following table shows how DynamoDB attribute types are mapped to .NET types. The table has two columns for .NET types. The first column describes the default .NET type used when no target type is provided. The second column describes what types are supported for (de)serializing the corresponding DynamoDB attribute values.

**Notes:**
* All .NET types must be concrete and have a parameter-less constructor.
* Only public instance properties are (de)serialized.
* Enum types are (de)serialized as strings by default.

|DynamoDB Attribute Type |Default .NET Type                      |Alternative .NET Types                 |
|------------------------|---------------------------------------|---------------------------------------|
|`B` (Binary)            |`byte[]`                               |N/A
|`BOOL` (Boolean)        |`bool`                                 |`bool?`
|`BS` (Binary Set)       |`HashSet<byte[]>`                      |implementation of `ISet<byte[]>`
|`L` (List)              |`List<object>`                         |implementation of `IList<T>` where `T` is a concrete type with a parameter-less constructor
|`M` (Map)               |`Dictionary<string, object>`           |any concrete type with a parameter-less constructor -OR- implementation of `IDictionary<string, T>` where `T` is a concrete type with a parameter-less constructor
|`N` (Number)            |`double`                               |`int`, `int?`, `long`, `long?`, `double?`, `decimal`, `decimal?`, `DateTimeOffset`, `DateTimeOffset?`
|`NS` (Number Set)       |`HashSet<double>`                      |implementation of `ISet<int>`, `ISet<long>`, `ISet<double>`, or `ISet<decimal>`
|`NULL` (Null)           |N/A (`null` doesn't have a .NET type)  |any class
|`S` (String)            |`string`                               |any enum type
|`SS` (String Set)       |`HashSet<string>`                      |implementation of `ISet<string>`

## String, Number, and Binary Sets

DynamoDB does not support storing empty sets. Therefore, the library will skip serializing empty instances of `ISet<string>`, `ISet<int>` etc. When a .NET class deserialized, the library detects the missing attribute value and initializes the instance property with an empty instance of `HashSte<string>`, `HashSet<int>`, etc. This ensures proper round-tripping behavior for DynamoDB sets while also avoiding unnecessary null checks in code.

In addition, instances of `HashSet<byte[]>` are initialized with an instance of `ByteArrayEqualityComparer` to provide a similar behavior as when serialized.

```csharp
class MyRecord {

    //--- Properties ---
    public HashSet<string> StringSet { get; set; }
    public HashSet<int> IntSet { get; set; }
    public HashSet<long> LongSet { get; set; }
    public HashSet<double> DoubleSet { get; set; }
    public HashSet<decimal> DecimalSet { get; set; }
    public HashSet<byte[]> ByteArraySet { get; set; }
}

var record = new MyRecord {
    StringSet = new HashSet<string>(),
    IntSet = new HashSet<int>(),
    LongSet = new HashSet<long>(),
    DoubleSet = new HashSet<double>(),
    DecimalSet = new HashSet<decimal>(),
    ByteArraySet = new HashSet<byte[]>(ByteArrayEqualityComparer.Instance)
};

var serialized = DynamoSerializer.Serialize(record);

var deserialized = DynamoSerializer.Deserialize(serialized);

Assert.IsNotNull(deserialized.StringSet);
Assert.IsNotNull(deserialized.IntSet);
Assert.IsNotNull(deserialized.LongSet);
Assert.IsNotNull(deserialized.DoubleSet);
Assert.IsNotNull(deserialized.DecimalSet);
Assert.IsNotNull(deserialized.ByteArraySet);
```

## `JsonElement` to `AttributeValue` Support

`DynamoSerializer` supports converting `JsonElement` values to DynamoDB attribute values, but not the other way around.

This makes it possible to convert an arbitrary JSON document to a DynamoDB document without having to first convert all occurrences of the `JsonElement` instances.

```csharp

// deserializing an arbitrary JSON document creates JsonElement values in nested objects
var deserializedJson = JsonSerializer.Deserialize<Dictionary, object>(json);

// JsonElement values are converted to corresponding DynamoDB attribute values
var dynamoDocument = DynamoSerializer.Serialize(deserializedJson);
```