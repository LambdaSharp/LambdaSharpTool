> TODO: move to correct location

> TODO: add `[DynamoPropertyName("foo")]` attribute (test with `"_t"` and `"_m"`)

> TODO: add `[DynamoPropertyIgnore]` attribute (test with `"_t"` and `"_m"`)

> TODO: empty string and binary values are now allowed: https://aws.amazon.com/about-aws/whats-new/2020/05/amazon-dynamodb-now-supports-empty-values-for-non-key-string-and-binary-attributes-in-dynamodb-tables/

> TODO: `Serialize()` can return `null` if the item should not be serialized

> TODO: test what happens when serializing an empty set in a list (???)

> TODO: `HashSet<byte[]>` is instantiated with a byte array comparer

# LambdaSharp.DynamoDB.Serialization

This utility library serializes C# data-structures to DynamoDB attribute values. It follows similar conventions as _System.Text.Json.Serialization_ classes.

## Sample Usage

### Code
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
     "Active": {
       "BOOL": true
     },
     "Binary": {
       "B": "Qnll"
     },
     "Name": {
       "S": "John Doe"
     },
     "Age": {
       "N": 42
     },
     "List": {
       "L": [
         {
           "M": {
             "Message": {
               "S": "Hello"
             }
           }
         },
         {
           "S": "World!"
         }
       ]
     },
     "Dictionary": {
       "M": {
         "Key": {
           "S": "Value"
         }
       }
     },
     "StringSet": {
       "SS": [
         "Red",
         "Blue"
       ]
     },
     "NumberSet": {
       "NS": [
         123,
         456
       ]
     },
     "BinarySet": {
       "BS": [
         "R29vZA==",
         "RGF5"
       ]
     }
   }
}
```

## Custom Type Converters

The `Serialize()` and `Deserialize()` methods take an optional options parameter to control serialization, as well as add custom data converters when needed.

```csharp
using LambdaSharp.DynamoDB.Serialization;

var attributeValue = DynamoSerializer.Serialize(data, new DynamoSerializerOptions {
    Converters = {
        new DynamoTimeSpanConverter()
    }
});
```

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

### Sample Converter

The following converter serializes `TimeSpan` from and to an `AttributeValue` instance.

```csharp
class DynamoTimeSpanConverter : ADynamoAttributeConverter {

    //--- Methods ---

    // determine if the presented type can be converted by this class
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TimeSpan);

    // convert presented value to a DynamoDB attribute value
    public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options)
        => new AttributeValue {
            N = ((TimeSpan)value).TotalSeconds.ToString(CultureInfo.InvariantCulture)
        };

    // convert from a DynamoDB attribute value to the expected type
    public override object FromNumber(string value, Type targetType, DynamoSerializerOptions options)
        => TimeSpan.FromSeconds(double.Parse(value, CultureInfo.InvariantCulture));
}
```