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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using FluentAssertions;
using LambdaSharp.DynamoDB.Native.Internal;
using Test.LambdaSharp.DynamoDB.Internal.DynamoRequestConverterTests.Model;
using Xunit;
using Xunit.Abstractions;

namespace Test.LambdaSharp.DynamoDB.Internal.DynamoRequestConverterTests {

    public class TryParseLiteral {

        //--- Class Methods ---
        private static Expression LambdaBody<T>(Expression<Func<MyRecord, T>> expression)
            => expression.Body;

        //--- Constructors ---
        public TryParseLiteral(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Null() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => (string)null);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].NULL.Should().BeTrue();
        }

        [Fact]
        public void Bool() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => true);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsBOOLSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].BOOL.Should().BeTrue();
        }

        [Fact]
        public void Binary() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => Encoding.UTF8.GetBytes("Hello"));
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].B.Should().NotBeNull();
            converter.ExpressionValues[":v_1"].B.ToArray().Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Hello"));
        }

        [Fact]
        public void String() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => "Hello");
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].S.Should().NotBeNull();
            converter.ExpressionValues[":v_1"].S.Should().Be("Hello");
        }

        [Fact]
        public void Int() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => 10);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().NotBeNull();
            converter.ExpressionValues[":v_1"].N.Should().Be("10");
        }

        [Fact]
        public void Long() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => 10L);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().NotBeNull();
            converter.ExpressionValues[":v_1"].N.Should().Be("10");
        }

        [Fact]
        public void Double() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => 10.0d);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().NotBeNull();
            converter.ExpressionValues[":v_1"].N.Should().Be("10");
        }

        [Fact]
        public void Decimal() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => 10m);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().NotBeNull();
            converter.ExpressionValues[":v_1"].N.Should().Be("10");
        }

        [Fact]
        public void List_empty() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new List<string>());
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsLSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].L.Count.Should().Be(0);
        }

        [Fact]
        public void List_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new List<string> { "Hello" });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsLSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].L.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].L[0].S.Should().Be("Hello");
        }


        [Fact]
        public void ArrayList_empty() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new ArrayList());
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsLSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].L.Count.Should().Be(0);
        }

        [Fact]
        public void ArrayList_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new ArrayList { "Hello" });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsLSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].L.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].L[0].S.Should().Be("Hello");
        }

        [Fact]
        public void Array_empty() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new string[0]);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsLSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].L.Count.Should().Be(0);
        }

        [Fact]
        public void Array_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new string[] { "Hello" });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsLSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].L.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].L[0].S.Should().Be("Hello");
        }

        [Fact]
        public void Map_empty() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new Dictionary<string, object>());
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsMSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].M.Count.Should().Be(0);
        }

        [Fact]
        public void Map_with_initializer_as_closure() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var closure = new Dictionary<string, object> {
                ["Key"] = "Value"
            };
            var expression = LambdaBody(record => closure);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].IsMSet.Should().BeTrue();
            converter.ExpressionValues[":v_1"].M.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].M.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].M.ContainsKey("Key").Should().BeTrue();
            converter.ExpressionValues[":v_1"].M["Key"].S.Should().Be("Value");
        }

        [Fact]
        public void HashSetString_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new HashSet<string> { "Hello" });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].SS.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].SS.Contains("Hello").Should().BeTrue();
        }

        [Fact]
        public void HashSetInt_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new HashSet<int> { 123 });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].NS.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].NS.Contains("123").Should().BeTrue();
        }

        [Fact]
        public void HashSetLong_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new HashSet<long> { 123L });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].NS.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].NS.Contains("123").Should().BeTrue();
        }

        [Fact]
        public void HashSetDouble_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new HashSet<double> { 123d });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].NS.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].NS.Contains("123").Should().BeTrue();
        }

        [Fact]
        public void HashSetDecimal_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new HashSet<decimal> { 123m });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].NS.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].NS.Contains("123").Should().BeTrue();
        }


        [Fact]
        public void HashSetBinary_with_initializer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => new HashSet<byte[]> { Encoding.UTF8.GetBytes("Hello") });
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].BS.Count.Should().Be(1);
            converter.ExpressionValues[":v_1"].BS.Should().Contain(item => item.ToArray().SequenceEqual(Encoding.UTF8.GetBytes("Hello")));
        }

        [Fact]
        public void Enum() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => MyEnum.EnumValue);
            var success = converter.TryParseLiteral(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be($":v_1");
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].S.Should().NotBeNull();
            converter.ExpressionValues[":v_1"].S.Should().Be("EnumValue");
        }
    }
}
