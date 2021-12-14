---
title: Custom Type Converters for DynamoDB - LambdaSharp
description: Serializing data structures with LambdaSharp.DynamoDB.Serialization
keywords: serialization, api, dynamodb, aws, amazon
---
# Custom Type Converters for DynamoDB

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
