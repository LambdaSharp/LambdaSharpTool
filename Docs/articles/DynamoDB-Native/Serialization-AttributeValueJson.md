---
title: .NET Type Mappings for DynamoDB - LambdaSharp
description: Serializing data structures with LambdaSharp.DynamoDB.Serialization
keywords: serialization, api, dynamodb, aws, amazon
---

# Miscellaneous

## `AttributeValue` to JSON converter for `System.Text.Json.JsonSerializer`

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

