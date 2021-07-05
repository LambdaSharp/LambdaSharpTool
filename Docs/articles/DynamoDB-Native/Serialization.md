---
title: Serialization in DynamoDB for .NET - LambdaSharp
description: Serializing data structures with LambdaSharp.DynamoDB.Serialization
keywords: serialization, api, dynamodb, aws, amazon
---
# Serialization in DynamoDB for .NET

## Overview

DynamoDB uses its own document model for serializing data structures, which looks similar to JSON. The _LambdaSharp.DynamoDB.Serialization_ library is modeled after other serialization libraries, such as _System.Text.Json.JsonSerializer_ to streamline conversions between .NET types and DynamoDB documents.

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

### String, Number, and Binary Sets

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

### Partial `JsonElement` Support

`DynamoSerializer` supports converting `JsonElement` values to DynamoDB attribute values, but not the other way around.

This makes it possible to convert an arbitrary JSON document to a DynamoDB document without having to first convert all occurrences of the `JsonElement` instances.

```csharp

// deserializing an arbitrary JSON document creates JsonElement values in nested objects
var deserializedJson = JsonSerializer.Deserialize<Dictionary, object>(json);

// JsonElement values are converted to corresponding DynamoDB attribute values
var dynamoDocument = DynamoSerializer.Serialize(deserializedJson);
```


## Serialization Attributes

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


## Custom Converters

The serialization logic can be extended by defining custom type converters either by implementing the `IDynamoAttributeConverter` interface or deriving from the `ADynamoAttributeConverter` base class.

The converter must implement the `CanConvert()`, the `ToAttributeValue()`, and one of the deserialization methods:
* `FromBool()` to convert from `AttributeValue.BOOL`
* `FromBinary()` to convert from `AttributeValue.B`
* `FromNumber()` to convert from `AttributeValue.NS`
* `FromString()` to convert from `AttributeValue.S`
* `FromList()` to convert from `AttributeValue.L`
* `FromMap()` to convert from `AttributeValue.M`
* `FromBinarySet()` to convert from `AttributeValue.BS`
* `FromNumberSet()` to convert from `AttributeValue.NS`
* `FromStringSet()` to convert from `AttributeValue.SS`

Optionally, the converter can also implement `GetDefaultValue()` to control the default value used to initialize a property that was not deserialized.

## Sample Custom Converter: `DynamoTimeSpanConverter`
The following converter serializes `TimeSpan` from and to an `AttributeValue` instance.

```csharp
class DynamoTimeSpanConverter : ADynamoAttributeConverter {

    //--- Methods ---
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TimeSpan);

    public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options)
        => new AttributeValue {
            N = ((TimeSpan)value).TotalSeconds.ToString(CultureInfo.InvariantCulture)
        };

    public override object FromNumber(string value, Type targetType, DynamoSerializerOptions options) {
        if(!double.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedValue)) {
            throw new DynamoSerializationException("invalid value for TimeSpan");
        }
        return TimeSpan.FromSeconds(parsedValue);
    }
}
```

The custom converter must then be added to the serialization options to become available.

```csharp
var document = DynamoSerializer.Serialize(record, new DynamoSerializerOptions {
    Converters = {
        new DynamoTimeSpanConverter()
    }
});
```


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
* `DynamoIDictionarySetConverter`
* `DynamoListConverter`
* `DynamoJsonElementConverter`
* `DynamoObjectConverter`

</dd>

</dl>


## Misc: `AttributeValue` to JSON converter for `System.Text.Json.JsonSerializer`

The `DynamoAttributeValueConverter` class is useful for debugging DynamoDB documents by providing accurate JSON serialization for `AttributeValue` instances.

```csharp

// serialize an instance to DynamoDB document
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

// serialize DynamoDB document
var json = JsonSerializer.Serialize(serialized, new JsonSerializerOptions {
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    Converters = {
        new DynamoAttributeValueConverter()
    }
});
```

**Output:**
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

