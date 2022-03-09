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
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using LambdaSharp.DynamoDB.Native.Internal;
using Test.LambdaSharp.DynamoDB.Internal.DynamoRequestConverterTests.Model;
using Xunit;
using Xunit.Abstractions;

namespace Test.LambdaSharp.DynamoDB.Internal.DynamoRequestConverterTests {

    public class TryParseValue {

        //--- Class Methods ---
        private static Expression LambdaBody<T>(Expression<Func<MyRecord, T>> expression)
            => expression.Body;

        //--- Constructors ---
        public TryParseValue(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Value_is_literal() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => 42);
            var success = converter.TryParseValue(expression, out var output, out var precedence);

            // assert
            success.Should().BeTrue();
            output.Should().Be(":v_1");
            converter.ExpressionAttributes.Any().Should().BeFalse();
            precedence.Should().Be(DynamoRequestConverter.Precedence.Atomic);
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().Be("42");
        }

        [Fact]
        public void Value_is_closure() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var closure = 42;
            var expression = LambdaBody(record => closure);
            var success = converter.TryParseValue(expression, out var output, out var precedence);

            // assert
            success.Should().BeTrue();
            output.Should().Be(":v_1");
            precedence.Should().Be(DynamoRequestConverter.Precedence.Atomic);
            converter.ExpressionAttributes.Any().Should().BeFalse();
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().Be("42");
        }

        [Fact]
        public void Value_is_literal_addition() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.Nested.Age + 42);
            var success = converter.TryParseValue(expression, out var output, out var precedence);

            // assert
            success.Should().BeTrue();
            output.Should().Be("Nested.Age + :v_1");
            precedence.Should().Be(DynamoRequestConverter.Precedence.ScalarAddSubtract);
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().Be("42");
        }

        [Fact]
        public void Value_is_literal_subtraction() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var expression = LambdaBody(record => record.Nested.Age - 42);
            var success = converter.TryParseValue(expression, out var output, out var precedence);

            // assert
            success.Should().BeTrue();
            output.Should().Be("Nested.Age - :v_1");
            precedence.Should().Be(DynamoRequestConverter.Precedence.ScalarAddSubtract);
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().Be("42");
        }

        [Fact]
        public void Value_is_closure_addition() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var closure = 42;
            var expression = LambdaBody(record => record.Nested.Age + closure);
            var success = converter.TryParseValue(expression, out var output, out var precedence);

            // assert
            success.Should().BeTrue();
            output.Should().Be("Nested.Age + :v_1");
            precedence.Should().Be(DynamoRequestConverter.Precedence.ScalarAddSubtract);
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().Be("42");
        }

        [Fact]
        public void Value_is_closure_subtraction() {

            // arrange
            var converter = new DynamoRequestConverter(new(), new(), new());

            // act
            var closure = 42;
            var expression = LambdaBody(record => record.Nested.Age - closure);
            var success = converter.TryParseValue(expression, out var output, out var precedence);

            // assert
            success.Should().BeTrue();
            output.Should().Be("Nested.Age - :v_1");
            precedence.Should().Be(DynamoRequestConverter.Precedence.ScalarAddSubtract);
            converter.ExpressionValues.ContainsKey(":v_1").Should().BeTrue();
            converter.ExpressionValues[":v_1"].N.Should().Be("42");
        }
    }
}
