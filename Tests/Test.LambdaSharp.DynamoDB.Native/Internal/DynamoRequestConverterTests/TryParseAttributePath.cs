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
using System.Linq.Expressions;
using FluentAssertions;
using LambdaSharp.DynamoDB.Native.Internal;
using Test.LambdaSharp.DynamoDB.Internal.DynamoRequestConverterTests.Model;
using Xunit;
using Xunit.Abstractions;

namespace Test.LambdaSharp.DynamoDB.Internal.DynamoRequestConverterTests {

    public class TryParseAttributePath {

        //--- Class Methods ---
        private static Expression LambdaBody<T>(Expression<Func<MyRecord, T>> expression)
            => expression.Body;

        //--- Constructors ---
        public TryParseAttributePath(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Attribute_path_for_class_property() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.Name);
            var success = converter.TryParseAttributePath(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be("#a_1");
            converter.ExpressionAttributes.ContainsKey("#a_1").Should().BeTrue();
            converter.ExpressionAttributes["#a_1"].Should().Be("Name");
        }

        [Fact]
        public void Attribute_path_for_nested_class_property() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.Nested.Age);
            var success = converter.TryParseAttributePath(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be("Nested.Age");
        }

        [Fact]
        public void Attribute_path_for_array_indexer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.Array[5]);
            var success = converter.TryParseAttributePath(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be("#a_1[5]");
            converter.ExpressionAttributes.ContainsKey("#a_1").Should().BeTrue();
            converter.ExpressionAttributes["#a_1"].Should().Be("Array");
        }

        [Fact]
        public void Attribute_path_for_arraylist_indexer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.ArrayList[5]);
            var success = converter.TryParseAttributePath(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be("ArrayList[5]");
        }

        [Fact]
        public void Attribute_path_for_list_indexer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.List[3]);
            var success = converter.TryParseAttributePath(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be("#a_1[3]");
            converter.ExpressionAttributes.ContainsKey("#a_1").Should().BeTrue();
            converter.ExpressionAttributes["#a_1"].Should().Be("List");
        }

        [Fact]
        public void Attribute_path_for_dictionary_indexer() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.Map["key"]);
            var success = converter.TryParseAttributePath(expression, out var output);

            // assert
            success.Should().BeTrue();
            output.Should().Be("#a_1.#a_2");
            converter.ExpressionAttributes.ContainsKey("#a_1").Should().BeTrue();
            converter.ExpressionAttributes["#a_1"].Should().Be("Map");
            converter.ExpressionAttributes.ContainsKey("#a_2").Should().BeTrue();
            converter.ExpressionAttributes["#a_2"].Should().Be("key");
        }
    }
}
