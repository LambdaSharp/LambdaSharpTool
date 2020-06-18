---
title: JSON Serialization - LambdaSharp
description: Best practices for JSON Serialization in LambdaSharp
keywords: newtonsoft, json, serialization, system
---

# JSON Serialization

> TODO:
> * describe `[JsonConverter(typeof(JsonIntConverter))] public int Value { get; set; }`
> * describe `[JsonConverter(typeof(JsonUnixDateTimeConverter))]`

As of _v0.8.1.0_, LambdaSharp has switched from using _Newtonsoft.Json_ to _System.Text.Json_. See the section below on how to migrate existing code to the new JSON serializer.

## Migrating JSON Serialization from _Newtonsoft.Json_ to _System.Text.Json_

Microsoft has published an excellent [migration guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to) for switching from _Newtonsoft.Json_ to _System.Text.Json_. In addition to the guide, the following sections explain how to migrate existing data-structures.

### Update Projects

Upgrade projects to .NET Core 3.1 by changing the target framework in the _.csproj_ file.
* Before: `<TargetFramework>netcoreapp2.1</TargetFramework>`
* After: `<TargetFramework>netcoreapp3.1</TargetFramework>`

Remove all _Newtonsoft.Json_ package dependencies.
* Remove: `<PackageReference Include="Newtonsoft.Json" Version="12.0.3" />`

### Remove Serializer Declarations

Remove all explicit `LambdaSerializer` declarations in the function files.
* Remove: `[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]`

### Replace Fields with Public Properties

Fields must be converted to public, mutable properties.
* Before: `public string Name;`
* After: `public string Name { get; set; }`

Non-public properties must be converted to public, mutable properties.
* Before: `internal string Name { get; set; };`
* After: `public string Name { get; set; }`

Limited mutable properties must be converted to public, mutable properties to be deserialized properly.
* Before: `public string Name { get; protected set; };`
* After: `public string Name { get; set; }`

### Review Property Types

> TODO: verify if this was `string` to `int` -or- `int` to `string`

Beware of `string` properties to deserialize a JSON number. _Newtonsoft.Json_ would automatically convert the number to a string. _System.Text.Json_ ignores the number instead.
* Before: `public string Timestamp { get; set; }`
* After: `public long Timestamp { get; set; }`

Beware of derived classes during serialization. _System.Text.Json_ will only serialize properties of the declared type, not all the properties of the actual instance, unless you use `object` as type.
* Before: `JsonSerializer.Serialize<Car>(new Sedan { ... })` (only `Car` properties are serialized; any additional `Sedan` properties are skipped)
* After: `JsonSerializer.Serialize<object>(mySedan)` (all public properties are always serialized)

### Update Property Attributes

Replace attribute for explicitly naming JSON elements.
* Before: `[JsonProperty("name")]`
* After: `[JsonPropertyName("name")]`
* Requires: `using System.Text.Json.Serialization;`

Replace attribute for requiring a JSON property (used by JSON schema generator for API Gateway models)
* Before: `[JsonRequired]` -or- `[JsonProperty(Required = Required.DisallowNull)]`
* After: `[DataMember(IsRequired = true)]`
* Requires: `using System.Runtime.Serialization;`

### Replace JSON Converters

Replace enum-to-string converters.
* Before: `[JsonConverter(typeof(StringEnumConverter))]`
* After: `[JsonConverter(typeof(JsonStringEnumConverter))]`
* Requires: `using System.Text.Json.Serialization;`

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