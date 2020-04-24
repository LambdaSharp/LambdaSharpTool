---
title: LambdaSharp "Hicetas" Release (v0.8)
description: Release notes for LambdaSharp "Hicetas" (v0.8)
keywords: release, notes, hicetas
---

# LambdaSharp "Hicetas" Release (v0.8.0.0) - TBD

> Hicetas was a Greek philosopher of the Pythagorean School. He was born in Syracuse. Like his fellow Pythagorean Ecphantus and the Academic Heraclides Ponticus, he believed that the daily movement of permanent stars was caused by the rotation of the Earth around its axis. When Copernicus referred to Nicetus Syracusanus (Nicetus of Syracuse) in _De revolutionibus orbium coelestium_ as having been cited by Cicero as an ancient who also argued that the Earth moved, it is believed that he was actually referring to Hicetas. [(Wikipedia)](https://en.wikipedia.org/wiki/Hicetas)

## What's New

TODO:

* LambdaSharp SDK
    * Ported `LambdaSharp` assembly to .NET Core 3.1 with null-aware support.

## BREAKING CHANGES

* Switched to `System.Text.Json` for serialization.
    * Replace `[JsonProperty]` with `[JsonPropertyName()]` (requires `using System.Text.Json.Serialization;`).
    * Replace `[JsonRequired]` with `[DataMember(IsRequired = true)]` (requires `using System.Runtime.Serialization;`).
    * Replace `[JsonProperty(Required = Required.DisallowNull)]` with `[DataMember(IsRequired = true)]` (requires `using System.Runtime.Serialization;`).
    * Replace `<TargetFramework>netcoreapp2.1</TargetFramework>` with `<TargetFramework>netcoreapp3.1</TargetFramework>`.
    * Replace `[JsonConverter(typeof(JsonStringEnumConverter))]` with `[JsonConverter(typeof(JsonStringEnumConverter))]` (requires `using System.Text.Json.Serialization;`).
    * Replace `SerializeJson` with `LambdaSerializer.Serialize`.
    * Replace `DeserializeJson` with `LambdaSerializer.Serialize`.
    * Replace fields with properties! (SUPER IMPORTANT!!!)
    * Beware of `string` properties to deserialize a JSON number. That won't work anymore.
* Removed `SerializeJson()` and `DeserializeJson()`; use `LambdaSerialize.Serialize()` and `LambdaSerializer.Deserialize()` respectively.
* Removed `Newtonsoft.Json` dependency from `LambdaSharp.dll`
* Replace `[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]` with `[assembly: LambdaSerializer(typeof(LambdaSharp.Serialization.LambdaJsonSerializer))]`


## New LambdaSharp CLI Features

## New LambdaSharp Assembly Features

### LambdaSharp.Core
    ...

