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
using System.Linq;
using System.Text;
using FluentAssertions;
using LambdaSharp.DynamoDB.Serialization;
using Test.LambdaSharp.DynamoDB.Serialization.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Test.LambdaSharp.DynamoDB.Serialization.DynamoConverterTests {

    public class Serialize {

        //--- Types ---
        public class MyCustomType {

            //--- Properties ---

            [DynamoPropertyIgnore]
            public string IgnoreText { get; set; }

            [DynamoPropertyName("OtherName")]
            public string CustomName { get; set; }
        }

        //--- Constructors ---
        public Serialize(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Serialize_anonymous_class() {
            var good = Encoding.UTF8.GetBytes("Good");
            var day = Encoding.UTF8.GetBytes("Day");

            // arrange
            var value = new {
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
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();

            // check 'Active' property
            attribute.M.ContainsKey(nameof(value.Active)).Should().BeTrue();
            attribute.M[nameof(value.Active)].BOOL.Should().BeTrue();

            // check 'Name' property
            attribute.M.ContainsKey(nameof(value.Name)).Should().BeTrue();
            attribute.M[nameof(value.Name)].S.Should().NotBeNull();
            attribute.M[nameof(value.Name)].S.Should().Be("John Doe");

            // check 'Age' property
            attribute.M.ContainsKey(nameof(value.Age)).Should().BeTrue();
            attribute.M[nameof(value.Age)].N.Should().NotBeNull();
            attribute.M[nameof(value.Age)].N.Should().Be("42");

            // check 'List' property
            attribute.M.ContainsKey(nameof(value.List)).Should().BeTrue();
            attribute.M[nameof(value.List)].IsLSet.Should().BeTrue();
            attribute.M[nameof(value.List)].L.Should().NotBeEmpty();
            attribute.M[nameof(value.List)].L.Count.Should().Be(2);

            // check 'Dictionary' property
            attribute.M.ContainsKey(nameof(value.Dictionary)).Should().BeTrue();
            attribute.M[nameof(value.Dictionary)].IsMSet.Should().BeTrue();
            attribute.M[nameof(value.Dictionary)].M.Should().NotBeEmpty();
            attribute.M[nameof(value.Dictionary)].M.Count.Should().Be(1);
            attribute.M[nameof(value.Dictionary)].M.ContainsKey("Key").Should().BeTrue();
            attribute.M[nameof(value.Dictionary)].M["Key"].S.Should().NotBeNull();
            attribute.M[nameof(value.Dictionary)].M["Key"].S.Should().Be("Value");

            // check 'StringSet' property
            attribute.M.ContainsKey(nameof(value.StringSet)).Should().BeTrue();
            attribute.M[nameof(value.StringSet)].SS.Should().NotBeEmpty();
            attribute.M[nameof(value.StringSet)].SS.Count.Should().Be(2);
            attribute.M[nameof(value.StringSet)].SS.Contains("Red").Should().BeTrue();
            attribute.M[nameof(value.StringSet)].SS.Contains("Blue").Should().BeTrue();

            // check 'NumberSet' property
            attribute.M.ContainsKey(nameof(value.NumberSet)).Should().BeTrue();
            attribute.M[nameof(value.NumberSet)].NS.Should().NotBeEmpty();
            attribute.M[nameof(value.NumberSet)].NS.Count.Should().Be(2);
            attribute.M[nameof(value.NumberSet)].NS.Contains("123").Should().BeTrue();
            attribute.M[nameof(value.NumberSet)].NS.Contains("456").Should().BeTrue();

            // check 'BinarySet' property
            attribute.M.ContainsKey(nameof(value.BinarySet)).Should().BeTrue();
            attribute.M[nameof(value.BinarySet)].BS.Should().NotBeEmpty();
            attribute.M[nameof(value.BinarySet)].BS.Count.Should().Be(2);
            attribute.M[nameof(value.BinarySet)].BS.Any(ms => ms.ToArray().SequenceEqual(good)).Should().BeTrue();
            attribute.M[nameof(value.BinarySet)].BS.Any(ms => ms.ToArray().SequenceEqual(day)).Should().BeTrue();
        }

        [Fact]
        public void Serialize_with_custom_converter() {

            // arrange
            var value = new {
                TimeSpan = TimeSpan.FromSeconds(789)
            };

            // act
            var attribute = DynamoSerializer.Serialize(value, new DynamoSerializerOptions {
                Converters = {
                    new DynamoTimeSpanConverter()
                }
            });


            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.TimeSpan)).Should().BeTrue();
            attribute.M[nameof(value.TimeSpan)].N.Should().NotBeNull();
            attribute.M[nameof(value.TimeSpan)].N.Should().Be("789");
        }

        [Fact]
        public void Serialize_empty_string() {

            // arrange
            var value = new {
                Text = ""
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.Text)).Should().BeTrue();
            attribute.M[nameof(value.Text)].S.Should().Be("");
        }

        [Fact]
        public void Serialize_empty_string_set() {

            // arrange
            var value = new {
                StringSet = new HashSet<string>()
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.StringSet)).Should().BeFalse();
        }

        [Fact]
        public void Serialize_empty_int_set() {

            // arrange
            var value = new {
                IntSet = new HashSet<int>()
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.IntSet)).Should().BeFalse();
        }

        [Fact]
        public void Serialize_empty_long_set() {

            // arrange
            var value = new {
                LongSet = new HashSet<long>()
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.LongSet)).Should().BeFalse();
        }

        [Fact]
        public void Serialize_empty_double_set() {

            // arrange
            var value = new {
                DoubleSet = new HashSet<double>()
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.DoubleSet)).Should().BeFalse();
        }

        [Fact]
        public void Serialize_empty_decimal_set() {

            // arrange
            var value = new {
                DecimalSet = new HashSet<decimal>()
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.DecimalSet)).Should().BeFalse();
        }

        [Fact]
        public void Serialize_empty_binary_set() {

            // arrange
            var value = new {
                BinarySet = new HashSet<byte[]>()
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.BinarySet)).Should().BeFalse();
        }

        [Fact]
        public void Serialize_custom_name_property() {

            // arrange
            var value = new MyCustomType {
                CustomName = "Hello"
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey("OtherName").Should().BeTrue();
            attribute.M["OtherName"].S.Should().Be("Hello");
        }

        [Fact]
        public void Serialize_ignore_property() {

            // arrange
            var value = new MyCustomType {
                IgnoreText = "World"
            };

            // act
            var attribute = DynamoSerializer.Serialize(value);

            // assert
            attribute.IsMSet.Should().BeTrue();
            attribute.M.ContainsKey(nameof(value.IgnoreText)).Should().BeFalse();
        }
    }
}
