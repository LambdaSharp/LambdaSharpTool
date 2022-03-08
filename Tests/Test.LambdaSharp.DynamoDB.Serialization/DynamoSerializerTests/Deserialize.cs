/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
 * lambdasharp.net
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using LambdaSharp.DynamoDB.Serialization;
using Test.LambdaSharp.DynamoDB.Serialization.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Test.LambdaSharp.DynamoDB.Serialization.DynamoConverterTests {

    public class Deserialize {

        // TODO (2021-07-14, bjorg): tests to add
        // * missing tests for M
        // * Test deserialization of custom type derived from `IList<Foo>`
        // * IList<T> handling in serialization
        // * enum handling in serialization
        // * add `[DynamoPropertyName("foo")]` attribute (test with `"_t"` and `"_m"`)
        // * add `[DynamoPropertyIgnore]` attribute (test with `"_t"` and `"_m"`)

        //--- Types ---
        private class MyType {

            //--- Properties ---
            public bool BoolValue { get; set; }
            public byte[] BinaryValue { get; set; }
            public int IntValue { get; set ;}
            public long LongValue { get; set; }
            public double DoubleValue { get; set; }
            public decimal DecimalValue { get; set; }
            public string StringValue { get; set; }
            public HashSet<byte[]> BinarySet { get; set; }
            public HashSet<string> StringSet { get; set; }
            public HashSet<int> IntSet { get; set; }
            public HashSet<long> LongSet { get; set; }
            public HashSet<double> DoubleSet { get; set; }
            public HashSet<decimal> DecimalSet { get; set; }
            public MyNestedType Nested { get; set; }
            public List<MyNestedType> TypedList { get; set; }
            public Dictionary<string, MyNestedType> TypedMap { get; set; }
        }

        private class MyNestedType {

            //--- Properties ---
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private class MyCustomType {

            //--- Properties ---

            [DynamoPropertyIgnore]
            public string IgnoreText { get; set; }

            [DynamoPropertyName("OtherName")]
            public string CustomName { get; set; }
        }

        public enum TestEnum {
            Nothing,
            Something
        }

        //--- Constructors ---
        public Deserialize(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        #region *** Deserialize Object w/ Properties ***
        [Fact]
        public void Deserialize_BOOL_value_as_boolean() {

            // arrange
            var attribute = new AttributeValue() {
                BOOL = true
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.BoolValue)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.BoolValue.Should().BeTrue();
        }

        [Fact]
        public void Deserialize_S_value_as_string() {

            // arrange
            var attribute = new AttributeValue() {
                S = "hello"
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.StringValue)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.StringValue.Should().Be("hello");
        }

        [Fact]
        public void Deserialize_N_value_as_int() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.IntValue)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.IntValue.Should().Be(123);
        }

        [Fact]
        public void Deserialize_N_value_as_long() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.LongValue)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.LongValue.Should().Be(123);
        }

        [Fact]
        public void Deserialize_N_value_as_double() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.DoubleValue)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.DoubleValue.Should().Be(123);
        }

        [Fact]
        public void Deserialize_N_value_as_decimal() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.DecimalValue)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.DecimalValue.Should().Be(123);
        }

        [Fact]
        public void Deserialize_B_value_as_byte_array() {

            // arrange
            var attribute = new AttributeValue() {
                B = new(new byte[] { 1, 2, 3 })
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.BinaryValue)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.BinaryValue.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Deserialize_BS_value_as_binary_hashset() {

            // arrange
            var attribute = new AttributeValue() {
                BS = new() {
                    new MemoryStream(new byte[] { 1, 2, 3 }),
                    new MemoryStream(new byte[] { 4, 5, 6 })
                }
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.BinarySet)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.BinarySet.Should().BeEquivalentTo(new[] {
                    new byte[] { 1, 2, 3 },
                    new byte[] { 4, 5, 6 }
                });
        }

        [Fact]
        public void Deserialize_SS_value_as_string_hashset() {

            // arrange
            var attribute = new AttributeValue() {
                SS = new() { "abc", "def" }
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.StringSet)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            value.Should().BeOfType<MyType>()
                .Which.StringSet.Should().BeEquivalentTo(new HashSet<string>(new[] { "abc", "def" }));
        }

        [Fact]
        public void Deserialize_M_value_as_custom_type() {

            // arrange
            var attribute = new AttributeValue() {
                M = new() {
                    [nameof(MyNestedType.Name)] = new() {
                        S = "John"
                    },
                    [nameof(MyNestedType.Age)] = new() {
                        N = "42"
                    }
                }
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.Nested)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            var which = value.Should().BeOfType<MyType>().Which;
            which.Nested.Should().NotBeNull();
            which.Nested.Name.Should().Be("John");
            which.Nested.Age.Should().Be(42);
        }

        [Fact]
        public void Deserialize_L_value_as_a_typed_list() {

            // arrange
            var attribute = new AttributeValue() {
                L = new() {
                    new() {
                        M = new() {
                            [nameof(MyNestedType.Name)] = new() {
                                S = "John"
                            },
                            [nameof(MyNestedType.Age)] = new() {
                                N = "42"
                            }
                        }
                    }
                }
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.TypedList)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            var which = value.Should().BeOfType<MyType>().Which;
            which.TypedList.Should().NotBeNull();
            which.TypedList.Count.Should().Be(1);
            which.TypedList[0].Should().NotBeNull();
            which.TypedList[0].Name.Should().Be("John");
            which.TypedList[0].Age.Should().Be(42);
        }

        [Fact]
        public void Deserialize_M_value_as_a_typed_dictionary() {

            // arrange
            var attribute = new AttributeValue() {
                M = new() {
                    ["First"] = new() {
                        M = new() {
                            [nameof(MyNestedType.Name)] = new() {
                                S = "John"
                            },
                            [nameof(MyNestedType.Age)] = new() {
                                N = "42"
                            }
                        }
                    }
                }
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyType.TypedMap)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyType));

            // assert
            var which = value.Should().BeOfType<MyType>().Which;
            which.TypedMap.Should().NotBeNull();
            which.TypedMap.Count.Should().Be(1);
            which.TypedMap.ContainsKey("First").Should().BeTrue();
            which.TypedMap["First"].Name.Should().Be("John");
            which.TypedMap["First"].Age.Should().Be(42);
        }

        [Fact]
        public void Deserialize_complex() {

            // arrange
            var attributes = new Dictionary<string, AttributeValue> {
                ["String"] = new("abc"),
                ["Number"] = new() {
                    N = "123"
                },
                ["List"] = new() {
                    L = new() {
                        new("def"),
                        new() {
                            N = "456"
                        }
                    }
                },
                ["Map"] = new() {
                    M = new() {
                        ["First"] = new("ghi"),
                        ["Second"] = new() {
                            N = "789"
                        }
                    }
                },
                ["Binary"] = new() {
                    B = new MemoryStream(Encoding.UTF8.GetBytes("Hello World!"))
                }
            };

            // act
            var result = DynamoSerializer.Deserialize(attributes, typeof(object));

            // assert
            var map = result.Should().BeOfType<Dictionary<string, object>>().Subject;
            map.Should().ContainKey("String")
                .WhoseValue.Should().Be("abc");
            map.Should().ContainKey("Number")
                .WhoseValue.Should().BeOfType<double>()
                .Which.Should().Be(123);
            map.Should().ContainKey("List")
                .WhoseValue.Should().BeOfType<List<object>>()
                .Which.Equals(new List<object> {
                    "def",
                    456.0d
                });
            map.Should().ContainKey("Map")
                .WhoseValue.Should().BeEquivalentTo(new Dictionary<string, object> {
                    ["First"] = "ghi",
                    ["Second"] = 789.0d
                });
            map.Should().ContainKey("Binary")
                .WhoseValue.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Hello World!"));
        }
        #endregion

        #region *** Deserialize Primitive AttributeValues ***

        [Fact]
        public void Deserialize_NULL_value() {

            // arrange
            var attribute = new AttributeValue() {
                NULL = true
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(object));

            // assert
            value.Should().BeNull();
        }

        [Fact]
        public void Deserialize_BOOL_value() {

            // arrange
            var attribute = new AttributeValue() {
                BOOL = true
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(object));

            // assert
            value.Should().BeOfType<bool>()
                .Subject.Should().BeTrue();
        }

        [Fact]
        public void Deserialize_BOOL_value_to_bool() {

            // arrange
            var attribute = new AttributeValue() {
                BOOL = true
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(bool));

            // assert
            value.Should().BeOfType<bool>()
                .Subject.Should().BeTrue();
        }

        [Fact]
        public void Deserialize_S_value() {

            // arrange
            var attribute = new AttributeValue() {
                S = "hello"
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(object));

            // assert
            value.Should().BeOfType<string>()
                .Subject.Should().Be("hello");
        }

        [Fact]
        public void Deserialize_S_value_to_string() {

            // arrange
            var attribute = new AttributeValue() {
                S = "hello"
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(string));

            // assert
            value.Should().BeOfType<string>()
                .Subject.Should().Be("hello");
        }

        [Fact]
        public void Deserialize_S_value_to_enum() {

            // arrange
            var attribute = new AttributeValue() {
                S = TestEnum.Something.ToString()
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(TestEnum));

            // assert
            value.Should().BeOfType<TestEnum>()
                .Subject.Should().Be(TestEnum.Something);
        }

        [Fact]
        public void Deserialize_N_value() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(object));

            // assert
            value.Should().BeOfType<double>()
                .Subject.Should().Be(123);
        }

        [Fact]
        public void Deserialize_N_value_to_int() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(int));

            // assert
            value.Should().BeOfType<int>()
                .Subject.Should().Be(123);
        }

        [Fact]
        public void Deserialize_N_value_to_long() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(long));

            // assert
            value.Should().BeOfType<long>()
                .Subject.Should().Be(123);
        }

        [Fact]
        public void Deserialize_N_value_to_double() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(double));

            // assert
            value.Should().BeOfType<double>()
                .Subject.Should().Be(123);
        }

        [Fact]
        public void Deserialize_N_value_to_decimal() {

            // arrange
            var attribute = new AttributeValue() {
                N = 123.ToString(CultureInfo.InvariantCulture)
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(decimal));

            // assert
            value.Should().BeOfType<decimal>()
                .Subject.Should().Be(123);
        }

        [Fact]
        public void Deserialize_B_value() {

            // arrange
            var attribute = new AttributeValue() {
                B = new(new byte[] { 1, 2, 3 })
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(object));

            // assert
            value.Should().BeOfType<byte[]>()
                .Subject.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        }

        [Fact]
        public void Deserialize_B_value_to_byte_array() {

            // arrange
            var attribute = new AttributeValue() {
                B = new(new byte[] { 1, 2, 3 })
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(byte[]));

            // assert
            value.Should().BeOfType<byte[]>()
                .Subject.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        }

        [Fact]
        public void Deserialize_L_value() {

            // arrange
            var attribute = new AttributeValue() {
                L = new() {
                    new() {
                        BOOL = true
                    },
                    new() {
                        S = "hello"
                    },
                    new() {
                        N = 123.ToString(CultureInfo.InvariantCulture)
                    }
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(object));

            // assert
            var list = value.Should().BeOfType<List<object>>().Subject;
            list.Should().HaveCount(3);
            list[0].Should().BeOfType<bool>()
                .Subject.Should().BeTrue();
            list[1].Should().BeOfType<string>()
                .Subject.Should().Be("hello");
            list[2].Should().BeOfType<double>()
                .Subject.Should().Be(123);
        }

        [Fact]
        public void Deserialize_L_value_to_string_list() {

            // arrange
            var attribute = new AttributeValue() {
                L = new() {
                    new() {
                        S = "hello"
                    },
                    new() {
                        S = "world"
                    },
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(List<string>));

            // assert
            var list = value.Should().BeOfType<List<string>>().Subject;
            list.Should().HaveCount(2);
            list[0].Should().Be("hello");
            list[1].Should().Be("world");
        }

        [Fact]
        public void Deserialize_L_value_to_int_list() {

            // arrange
            var attribute = new AttributeValue() {
                L = new() {
                    new() {
                        N = 123.ToString(CultureInfo.InvariantCulture)
                    },
                    new() {
                        N = 456.ToString(CultureInfo.InvariantCulture)
                    },
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(List<int>));

            // assert
            var list = value.Should().BeOfType<List<int>>().Subject;
            list.Should().HaveCount(2);
            list[0].Should().Be(123);
            list[1].Should().Be(456);
        }

        [Fact]
        public void Deserialize_SS_value_to_string_hashset() {

            // arrange
            var attribute = new AttributeValue() {
                SS = new() {
                    "Red",
                    "Blue"
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(HashSet<string>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<string>>().Subject;
            hashSet.Should().HaveCount(2);
            hashSet.Contains("Red").Should().BeTrue();
            hashSet.Contains("Blue").Should().BeTrue();
        }

        [Fact]
        public void Deserialize_NS_value_to_int_hashset() {

            // arrange
            var attribute = new AttributeValue() {
                NS = new() {
                    "123",
                    "456"
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(HashSet<int>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<int>>().Subject;
            hashSet.Should().HaveCount(2);
            hashSet.Contains(123).Should().BeTrue();
            hashSet.Contains(456).Should().BeTrue();
        }

        [Fact]
        public void Deserialize_NS_value_to_long_hashset() {

            // arrange
            var attribute = new AttributeValue() {
                NS = new() {
                    "123",
                    "456"
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(HashSet<long>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<long>>().Subject;
            hashSet.Should().HaveCount(2);
            hashSet.Contains(123).Should().BeTrue();
            hashSet.Contains(456).Should().BeTrue();
        }

        [Fact]
        public void Deserialize_NS_value_to_double_hashset() {

            // arrange
            var attribute = new AttributeValue() {
                NS = new() {
                    "123",
                    "456"
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(HashSet<double>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<double>>().Subject;
            hashSet.Should().HaveCount(2);
            hashSet.Contains(123).Should().BeTrue();
            hashSet.Contains(456).Should().BeTrue();
        }

        [Fact]
        public void Deserialize_NS_value_to_decimal_hashset() {

            // arrange
            var attribute = new AttributeValue() {
                NS = new() {
                    "123",
                    "456"
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(HashSet<decimal>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<decimal>>().Subject;
            hashSet.Should().HaveCount(2);
            hashSet.Contains(123).Should().BeTrue();
            hashSet.Contains(456).Should().BeTrue();
        }

        [Fact]
        public void Deserialize_BS_value_to_byte_array() {

            // arrange
            var hello = Encoding.UTF8.GetBytes("Hello");
            var world = Encoding.UTF8.GetBytes("World");
            var attribute = new AttributeValue() {
                BS = new() {
                    new MemoryStream(hello),
                    new MemoryStream(world)
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(HashSet<byte[]>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<byte[]>>().Subject;
            hashSet.Should().HaveCount(2);
            hashSet.Contains(hello).Should().BeTrue();
            hashSet.Contains(world).Should().BeTrue();
        }

        [Fact]
        public void Deserialize_missing_value_to_byte_array() {

            // arrange

            // act
            var value = DynamoSerializer.Deserialize(attribute: null, typeof(HashSet<byte[]>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<byte[]>>().Subject;
            hashSet.Should().HaveCount(0);
        }

        [Fact]
        public void Deserialize_missing_value_to_string_hashset() {

            // arrange

            // act
            var value = DynamoSerializer.Deserialize(attribute: null, typeof(HashSet<string>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<string>>().Subject;
            hashSet.Should().HaveCount(0);
        }

        [Fact]
        public void Deserialize_missing_value_to_int_hashset() {

            // arrange

            // act
            var value = DynamoSerializer.Deserialize(attribute: null, typeof(HashSet<int>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<int>>().Subject;
            hashSet.Should().HaveCount(0);
        }

        [Fact]
        public void Deserialize_missing_value_to_long_hashset() {

            // arrange

            // act
            var value = DynamoSerializer.Deserialize(attribute: null, typeof(HashSet<long>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<long>>().Subject;
            hashSet.Should().HaveCount(0);
        }

        [Fact]
        public void Deserialize_missing_value_to_double_hashset() {

            // arrange

            // act
            var value = DynamoSerializer.Deserialize(attribute: null, typeof(HashSet<double>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<double>>().Subject;
            hashSet.Should().HaveCount(0);
        }

        [Fact]
        public void Deserialize_missing_value_to_decimal_hashset() {

            // arrange

            // act
            var value = DynamoSerializer.Deserialize(attribute: null, typeof(HashSet<decimal>));

            // assert
            var hashSet = value.Should().BeOfType<HashSet<decimal>>().Subject;
            hashSet.Should().HaveCount(0);
        }
        #endregion

        [Fact]
        public void Deserialize_with_custom_converter() {

            // arrange
            var attribute = new AttributeValue() {
                N = "789"
            };

            // act
            var value = DynamoSerializer.Deserialize(attribute, typeof(TimeSpan), new DynamoSerializerOptions {
                Converters = {
                    new DynamoTimeSpanConverter()
                }
            });

            // assert
            var timespan = value.Should().BeOfType<TimeSpan>().Subject;
            timespan.TotalSeconds.Should().Be(789);
        }

        [Fact]
        public void Deserialize_custom_name_property() {

            // arrange
            var attribute = new AttributeValue() {
                S = "Hello"
            };
            var attributes = new AttributeValue() {
                M = new() {
                    ["OtherName"] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyCustomType));

            // assert
            var which = value.Should().BeOfType<MyCustomType>().Which;
            which.Should().NotBeNull();
            which.CustomName.Should().Be("Hello");
        }

        [Fact]
        public void Serialize_ignore_property() {

            // arrange
            var attribute = new AttributeValue() {
                S = "World"
            };
            var attributes = new AttributeValue() {
                M = new() {
                    [nameof(MyCustomType.IgnoreText)] = attribute
                }
            };

            // act
            var value = DynamoSerializer.Deserialize(attributes, typeof(MyCustomType));

            // assert
            var which = value.Should().BeOfType<MyCustomType>().Which;
            which.Should().NotBeNull();
            which.IgnoreText.Should().BeNull();
        }
    }
}
