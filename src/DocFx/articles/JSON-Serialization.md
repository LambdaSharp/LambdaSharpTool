---
title: JSON Serialization - LambdaSharp
description: Best practices for JSON Serialization in LambdaSharp
keywords: newtonsoft, json, serialization, system
---

# JSON Serialization

As of _v0.8.1.0_, LambdaSharp has switched from using `Newtonsoft.Json` to `System.Text.Json`. See the section below on how to migrate existing code to the new JSON serializer.

## Migrating JSON Serialization from Newtonsoft.Json to System.Text.Json

Microsoft has published an excellent [migration guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to) for switching from `Newtonsoft.Json` to `System.Text.Json`. In addition to the guide, the following sections explain how to migrate existing data-structures to get the most out of LambdaSharp.

### Project Dependencies

TODO:
* Replace `<TargetFramework>netcoreapp2.1</TargetFramework>` with `<TargetFramework>netcoreapp3.1</TargetFramework>`.
* Remove `<PackageReference Include="Newtonsoft.Json" Version="12.0.3" />`

### Replace Fields with Properties

TODO:
* Replace fields with properties! (SUPER IMPORTANT!!!)

### Replace Property Attributes

* Replace `[JsonProperty]` with `[JsonPropertyName()]` (requires `using System.Text.Json.Serialization;`).
* Replace `[JsonRequired]` with `[DataMember(IsRequired = true)]` (requires `using System.Runtime.Serialization;`).

### Review Property Converters

TODO:
* Beware of `string` properties to deserialize a JSON number. That won't work anymore.
* Replace `[JsonProperty(Required = Required.DisallowNull)]` with `[DataMember(IsRequired = true)]` (requires `using System.Runtime.Serialization;`).
* Replace `[JsonConverter(typeof(JsonStringEnumConverter))]` with `[JsonConverter(typeof(JsonStringEnumConverter))]` (requires `using System.Text.Json.Serialization;`).
