/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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

using FluentAssertions;
using LambdaSharp.Tool.Compiler.Parser.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Compiler.Parser {

    public class ParseListOfLiteralExpressionsTests : _Init {

        //--- Constructors ---
        public ParseListOfLiteralExpressionsTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void ParseListOfLiteralExpressions_SingleValue() {

            // arrange
            var parser = NewParser(
@"foo");

            // act
            var value = parser.ParseListOfLiteralExpressions();

            // assert
            ExpectNoMessages();
            value.Should().NotBeNull();
            value.Count.Should().Be(1);
            value[0].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("foo");
        }

        [Fact]
        public void ParseListOfLiteralExpressions_CommDelimitedValues() {

            // arrange
            var parser = NewParser(
@"foo, bar");

            // act
            var value = parser.ParseListOfLiteralExpressions();

            // assert
            ExpectNoMessages();
            value.Should().NotBeNull();
            value.Count.Should().Be(2);
            value[0].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("foo");
            value[1].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("bar");
        }

        [Fact]
        public void ParseListOfLiteralExpressions_ListOfValues() {

            // arrange
            var parser = NewParser(
@"- foo
- bar");

            // act
            var value = parser.ParseListOfLiteralExpressions();

            // assert
            ExpectNoMessages();
            value.Should().NotBeNull();
            value.Count.Should().Be(2);
            value[0].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("foo");
            value[1].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("bar");
        }
    }
}