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

    public class TryParseCondition {

        //--- Class Methods ---
        private static Expression LambdaBody<T>(Expression<Func<MyRecord, T>> expression)
            => expression.Body;

        //--- Constructors ---
        public TryParseCondition(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        [Fact]
        public void Enum_equal() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.Enum == MyEnum.EnumValue);
            var success = converter.TryParseCondition(expression, out var output, out _);

            // assert
            success.Should().BeTrue();
            output.Should().Be("Enum = :v_1");
            converter.ExpressionAttributes.Should().BeEmpty();
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].S.Should().Be("EnumValue");
        }

        [Fact]
        public void Enum_not_equal() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.Enum != MyEnum.EnumValue);
            var success = converter.TryParseCondition(expression, out var output, out _);

            // assert
            success.Should().BeTrue();
            output.Should().Be("Enum <> :v_1");
            converter.ExpressionAttributes.Should().BeEmpty();
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].S.Should().Be("EnumValue");
        }

        [Fact]
        public void Enum_equal_closure() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var rec = new MyRecord {
                Enum = MyEnum.EnumValue
            };
            var expression = LambdaBody(record => record.Enum == rec.Enum);
            var success = converter.TryParseCondition(expression, out var output, out _);

            // assert
            success.Should().BeTrue();
            output.Should().Be("Enum = :v_1");
            converter.ExpressionAttributes.Should().BeEmpty();
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].S.Should().Be("EnumValue");
        }
    }
}
