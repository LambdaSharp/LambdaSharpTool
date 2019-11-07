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
using LambdaSharp.Tool.Compiler.Parser;
using LambdaSharp.Tool.Compiler.Parser.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Compiler.Parser {

    public class ParseExpressionTests : _Init {

        //--- Constructors ---
        public ParseExpressionTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void ParseLiteralExpression() {

            // arrange
            var parser = NewParser(
@"text");

            // act
            var value = parser.ParseExpression();

            // assert
            ExpectNoMessages();
            value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("text");
        }

        [Fact]
        public void ParseSubFunctionExpression() {

            // arrange
            var parser = NewParser(
@"!Sub text");

            // act
            var value = parser.ParseExpression();

            // assert
            ExpectNoMessages();
            var sub = value.Should().BeOfType<SubFunctionExpression>().Which;
            sub.FormatString.Should().NotBeNull();
            sub.FormatString.Value.Should().Be("text");
            sub.Parameters.Should().BeNull();
        }

        [Fact]
        public void ParseShortFormAndLongFormFunctionExpressions() {

            // arrange
            var parser = NewParser(
@"!Base64
    Fn::Sub: text");

            // act
            var value = parser.ParseExpression();

            // assert
            ExpectNoMessages();
            var base64 = value.Should().BeOfType<Base64FunctionExpression>().Which;
            var sub = base64.Value.Should().BeOfType<SubFunctionExpression>().Which;
            sub.FormatString.Should().NotBeNull();
            sub.FormatString.Value.Should().Be("text");
            sub.Parameters.Should().BeNull();
        }

        [Fact]
        public void ParseLongFormAndShortFormFunctionExpressions() {

            // arrange
            var parser = NewParser(
@"Fn::Base64:
    !Sub text");


            // act
            var value = parser.ParseExpression();

            // assert
            ExpectNoMessages();
            var base64 = value.Should().BeOfType<Base64FunctionExpression>().Which;
            var sub = base64.Value.Should().BeOfType<SubFunctionExpression>().Which;
            sub.FormatString.Should().NotBeNull();
            sub.FormatString.Value.Should().Be("text");
            sub.Parameters.Should().BeNull();
        }

        [Fact]
        public void ParseNestedIncludes() {

            // arrange
            AddSource("test.yml", "!Include include.yml");
            AddSource("include.yml", "!Include include.txt");
            AddSource("include.txt", "hello world!");
            var parser = new LambdaSharpParser(Provider, "test.yml");

            // act
            var value = parser.ParseExpression();

            // assert
            ExpectNoMessages();
            value.Should().NotBeNull();
            var literal = value.Should().BeOfType<LiteralExpression>().Which;
            literal.Value.Should().Be("hello world!");
            literal.SourceLocation.FilePath.Should().Be("include.txt");
        }
    }
}