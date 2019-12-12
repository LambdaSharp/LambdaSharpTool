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
            sub.Parameters.Should().NotBeNull();
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
            sub.Parameters.Should().NotBeNull();
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
            sub.Parameters.Should().NotBeNull();
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

        [Fact]
        public void ParseLiteralCanonicalNullExpression() => ParseLiteralTest("~", "", LiteralType.Null);

        [Fact]
        public void ParseLiteralNullExpression() => ParseLiteralTest("null", "", LiteralType.Null);

        [Fact]
        public void ParseLiteralEmptyNullExpression() => ParseLiteralTest("", "", LiteralType.Null);

        [Fact]
        public void ParseLiteralCanonicalBoolExpression() => ParseLiteralTest("y", "true", LiteralType.Bool);

        [Fact]
        public void ParseLiteralAnswerBoolExpression() => ParseLiteralTest("NO", "false", LiteralType.Bool);

        [Fact]
        public void ParseLiteralLogicalBoolExpression() => ParseLiteralTest("True", "true", LiteralType.Bool);

        [Fact]
        public void ParseLiteralOptionBoolExpression() => ParseLiteralTest("on", "true", LiteralType.Bool);

        [Fact]
        public void ParseLiteralCanonicalIntegerExpression() => ParseLiteralTest("685230", "685230", LiteralType.Integer);

        [Fact]
        public void ParseLiteralDecimalIntegerExpression() => ParseLiteralTest("+685_230", "685230", LiteralType.Integer);

        [Fact]
        public void ParseLiteralOctalIntegerExpression() => ParseLiteralTest("02472256", "685230", LiteralType.Integer);

        [Fact]
        public void ParseLiteralHexadecimalIntegerExpression() => ParseLiteralTest("0x_0A_74_AE", "685230", LiteralType.Integer);

        [Fact]
        public void ParseLiteralBinaryIntegerExpression() => ParseLiteralTest("0b1010_0111_0100_1010_1110", "685230", LiteralType.Integer);

        [Fact]
        public void ParseLiteralSexagesimalIntegerExpression() => ParseLiteralTest("190:20:30", "685230", LiteralType.Integer);

        [Fact]
        public void ParseLiteralCanonicalFloatExpression() => ParseLiteralTest("6.8523015e+5", "685230.15", LiteralType.Float);

        [Fact]
        public void ParseLiteralExponentialFloatExpression() => ParseLiteralTest("685.230_15e+03", "685230.15", LiteralType.Float);

        [Fact]
        public void ParseLiteralFixedFloatExpression() => ParseLiteralTest("685_230.15", "685230.15", LiteralType.Float);

        [Fact]
        public void ParseLiteralSexagesimalFloatExpression() => ParseLiteralTest("190:20:30.15", "685230.15", LiteralType.Float);

        [Fact]
        public void ParseLiteralNegativeInfiityFloatExpression() => ParseLiteralTest("-.inf", "-∞", LiteralType.Float);

        [Fact]
        public void ParseLiteralNotANumberFloatExpression() => ParseLiteralTest(".NaN", "NaN", LiteralType.Float);

        [Fact]
        public void ParseLiteralCanonicalTimestampExpression() => ParseLiteralTest("2001-12-15T02:59:43.1Z", "12/15/2001 2:59:43 AM +00:00", LiteralType.Timestamp);

        [Fact]
        public void ParseLiteralIso8601TimestampExpression() => ParseLiteralTest("2001-12-14t21:59:43.10-05:00", "12/15/2001 2:59:43 AM +00:00", LiteralType.Timestamp);

        [Fact]
        public void ParseLiteralSpaceSeparaterTimestampExpression() => ParseLiteralTest("2001-12-14 21:59:43.10 -5", "12/15/2001 2:59:43 AM +00:00", LiteralType.Timestamp);

        [Fact]
        public void ParseLiteralNoTimeZoneTimestampExpression() => ParseLiteralTest("2001-12-15 2:59:43.10", "12/15/2001 2:59:43 AM +00:00", LiteralType.Timestamp);

        [Fact]
        public void ParseLiteralDateTimestampExpression() => ParseLiteralTest("2002-12-14", "12/14/2002 12:00:00 AM +00:00", LiteralType.Timestamp);

        private void ParseLiteralTest(string input, string output, LiteralType type) {

            // arrange
            var parser = NewParser($"Value: {input}");

            // act
            var value = parser.ParseExpression();

            // assert
            ExpectNoMessages();
            var map = value.Should().BeOfType<ObjectExpression>().Which;
            map.Count.Should().Be(1);
            var item = map.Items[0];
            var literal = item.Value.Should().BeOfType<LiteralExpression>().Which;
            literal.Value.Should().Be(output);
            literal.Type.Should().Be(type);
       }
    }
}