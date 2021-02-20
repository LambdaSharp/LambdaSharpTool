---
title: JSON Serialization - LambdaSharp
description: Best practices for JSON Serialization in LambdaSharp
keywords: newtonsoft, json, serialization, system
---

# JSON Serialization

Starting with v0.8.2, _LambdaSharp_ uses _System.Text.Json_ v5.0 instead of _Newtonsoft.Json_ for JSON serialization of built-in types. Custom types are handled with the JSON serializer specified using the `LambdaSerializer` assembly attribute.

This article describes how to switch from the default _Newtonsoft.Json_ to _System.Text.Json_, as well as what to look out for.

## Migrating JSON Serialization from _Newtonsoft.Json_ to _System.Text.Json_

Lambda functions using _System.Text.Json_ must declare `LambdaSystemTextJsonSerializer` as their JSON serializer using the `JsonSerializer` assembly attribute.
```csharp
[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(LambdaSharp.Serialization.LambdaSystemTextJsonSerializer))]
```

Microsoft has published an excellent [migration guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to?pivots=dotnet-5-0) for switching from _Newtonsoft.Json_ to _System.Text.Json_. In addition to the guide, the following sections explain how to migrate existing data-structures.

Alternatively, functions can continue to use _Newtonsoft.Json_ as their JSON serializer by including the `LambdaSharp.Serialization.NewtonsoftJson` assembly from NuGet:
```csharp
[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(LambdaSharp.Serialization.LambdaNewtonsoftJsonSerializer))]
```

### Update Projects

Upgrade projects to .NET Core 3.1 by changing the target framework in the _.csproj_ file.
* Before: `<TargetFramework>netcoreapp2.1</TargetFramework>`
* After: `<TargetFramework>netcoreapp3.1</TargetFramework>`

Remove all _Newtonsoft.Json_ package dependencies (version may vary).
* Remove: `<PackageReference Include="Newtonsoft.Json" />`

### Replace Non-Public Properties/Fields with Public Properties/Fields

Unlike _Newtonsoft.Json_, _System.Text.Json_ does not serialize non-public properties/fields.

Non-public properties must be converted to public, mutable properties.
* Before: `internal string Name { get; set; }`
* After: `public string Name { get; set; }`

Limited mutable properties must be converted to public, mutable properties to be deserialized properly.
* Before: `public string Name { get; protected set; }`
* After: `public string Name { get; set; }`

Non-public fields must be converted to public, mutable fields.
* Before: `internal string Name;`
* After: `public string Name;`

### Convert JSON string values to `enum` properties

_Newtonsoft.Json_ provides `StringEnumConverter` to convert JSON string to `enum` properties. _System.Text.Json_ includes an equivalent converter called `JsonStringEnumConverter` in the _System.Text.Json.Serialization_ namespace.
* Before: `[JsonConverter(typeof(StringEnumConverter))]`
* After: `[JsonConverter(typeof(JsonStringEnumConverter))]`

### Convert JSON integer values to `DateTimeOffset`/`DateTime` properties

_Newtonsoft.Json_ provides `UnixDateTimeConverter` to convert JSON integer to `DateTime` properties. _System.Text.Json_ does not include such a converter. Instead, _LambdaSharp.Serialization_ defines `JsonEpochSecondsDateTimeOffsetConverter` and `JsonEpochSecondsDateTimeConverter` to convert `DateTimeOffset` and `DateTime`, respectively to a JSON integer representing the UNIX epoch in seconds.

```csharp
[JsonConverter(typeof(JsonEpochSecondsDateTimeOffsetConverter))]
public DateTimeOffset Epoch { get; set; }
```

--OR--

```csharp
[JsonConverter(typeof(JsonEpochSecondsDateTimeConverter))]
public DateTime Epoch { get; set; }
```

### Update Property Attributes

Replace attribute for explicitly naming JSON elements.
* Before: `[JsonProperty("name")]`
* After: `[JsonPropertyName("name")]`
* Requires: `using System.Text.Json.Serialization;`

Replace attribute for requiring a JSON property (used by JSON schema generator for API Gateway models)
* Before: `[JsonRequired]` -or- `[JsonProperty(Required = Required.DisallowNull)]`
* After: `[Required]`
* Requires: `using System.ComponentModel.DataAnnotations;`

### Case-Sensitive Serialization

_Newtonsoft.Json_ is not case-sensitive on property/field names, but _System.Text.Json_ is.

#### Solution 1: Use _Newtonsoft.Json_ Serializer

Keep using the _Newtonsoft.Json_ serializer instead by adding the `LambdaSharp.Serialization.NewtonsoftJson` NuGet package and assembly attribute for it.

```csharp
[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(LambdaSharp.Serialization.LambdaNewtonsoftJsonSerializer))]`
```

#### Solution 2: Provide Proper Case-Sensitive Spelling for Property/Field

Use the `[JsonPropertyName("name")]` attribute to provide the property/field name with the case-sensitive spelling.

```csharp
class MyClass {

    //--- Properties ---
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
```

#### Solution 3: Custom _System.Text.Json_ Serializer Settings

Create a custom serializer that overrides the default _System.Text.Json_ behavior in its constructor.

```csharp
[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(MySerializer))]

public class MySerializer : LambdaSharp.Serialization.LambdaSystemTextJsonSerializer {

    //--- Constructors ---
    public MySerializer() : base(settings => {
        settings.settings.PropertyNameCaseInsensitive = true;
    }) { }
}
```

### Derived Classes Serialization

Beware of derived classes during serialization. _System.Text.Json_ will only serialize properties of the declared type, not all the properties of the actual instance, unless you use `object` as type.
* Before: `LambdaSerializer.Serialize<Car>(new Sedan { ... })` (only `Car` properties are serialized; any additional `Sedan` properties are skipped)
* After: `LambdaSerializer.Serialize<object>(mySedan)` (all public properties are serialized)

### Polymorphic Serialization

Additional care is required when serializing an abstract syntax tree where nodes share an abstract base definition.

For example, consider the definition for nestable lists with values. Using `Newtonsoft.Json`, we may have written something as follows, where `ListConverter` and `ValueConverter` are JSON-converters that implement serialization for their respective types.
```csharp
public class AExpression { }

[JsonConverter(ListConverter)]
public class List : AExpression {
    public List<AExpression> Items { get; set; } = new List<AExpression>()
}

[JsonConverter(LiteralConverter)]
public class Literal : AExpression {
    public string Value { get; set; }
}
```

Surprisingly, the following expression serializes as `[{}]` because the declared type for `List` is `AExpression` which has no properties!
```csharp
JsonSerialize.Serialize(new List {
    Items = {
        new Literal {
            Value = 123
        }
    }
})
```

The solution is to provide a `JsonConverter` for `AExpression` that knows how to perform polymorphic serialization based on the actual derived type.
```csharp
public class ExpressionConverter : JsonConverter<AExpression> {

    //--- Class Fields ---
    private readonly ListConverter _listSerializer = new ListConverter();
    private readonly LiteralConverter _literalSerializer = new LiteralConverter();

    //--- Methods ---
    public override ACloudFormationExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {

        // Read() implementation omitted for brevity
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, ACloudFormationExpression value, JsonSerializerOptions options) {
        switch(value) {
        case List list:
            _listSerializer.Write(writer, list, options);
            break;
        case Literal literal:
            _literalSerializer.Write(writer, literal, options);
            break;
        default:
            throw new ArgumentException($"unsupported serialization type {value?.GetType().FullName ?? "<null>"}");
        }
    }
}
```

## Custom JSON Serializer

Custom JSON serializer implementation can also be used by providing a class that derives from `ILambdaJsonSerializer`.

For example, the following JSON serializer uses [LitJSON](https://litjson.net/) instead.

```csharp
[assembly: Amazon.Lambda.Core.LambdaSerializer(typeof(MySerializer))]

public class MySerializer : ILambdaJsonSerializer {

    //--- Methods ---
    public object Deserialize(Stream stream, Type type) {
        var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return LitJson.JsonMapper.ToObject(json, type);
    }

    public T Deserialize<T>(Stream requestStream) => (T)Deserialize(stream, typeof(T));

    public void Serialize<T>(T response, Stream responseStream) {
        var json = LitJson.JsonMapper.ToJson(response);
        responseStream.Write(Encoding.UTF8.GetBytes(json));
    }
}
```