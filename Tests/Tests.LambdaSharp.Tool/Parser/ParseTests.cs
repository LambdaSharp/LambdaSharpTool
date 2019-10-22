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

using System.IO;
using System.Linq;
using FluentAssertions;
using LambdaSharp.Tool.Parser;
using LambdaSharp.Tool.Parser.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Parser {

    public class ParseTests {

        //--- Fields ---
        private readonly ITestOutputHelper _output;

        //--- Constructors ---
        public ParseTests(ITestOutputHelper output) => _output = output;

        //--- Methods ---

        [Fact]
        public void ParseModuleDeclaration() {

            // arrange
            var source =
@"Module: My.Module
Version: 1.2.3.4-DEV
Description: description
Pragmas:
    - pragma
Secrets:
    - secret
Using:
    - Module: My.OtherModule
      Description:
Items:
    - Resource: Foo
      Type: AWS::SNS::Topic
    - Variable: Bar
      Value: 123
";
            var parser = new LambdaSharpParser("<literal>", source);

            // act
            parser.Start();
            var module = parser.ParseDeclarationOf<ModuleDeclaration>();
            parser.End();

            // assert
            foreach(var message in parser.Messages) {
                _output.WriteLine(message);
            }
            parser.Messages.Any().Should().Be(false);
        }

        [Fact]
        public void ParseLiteralExpression() {

            // arrange
            var source =
@"text";
            var parser = new LambdaSharpParser("<literal>", source);

            // act
            parser.Start();
            var value = parser.ParseExpression();
            parser.End();

            // assert
            foreach(var message in parser.Messages) {
                _output.WriteLine(message);
            }
            parser.Messages.Any().Should().Be(false);
            value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("text");
        }


        [Fact]
        public void ParseSubFunctionExpression() {

            // arrange
            var source =
@"!Sub text";
            var parser = new LambdaSharpParser("<literal>", source);

            // act
            parser.Start();
            var value = parser.ParseExpression();
            parser.End();

            // assert
            foreach(var message in parser.Messages) {
                _output.WriteLine(message);
            }
            parser.Messages.Any().Should().Be(false);
            value.Should().BeOfType<SubFunctionExpression>();
            var sub = (SubFunctionExpression)value;
            sub.FormatString.Should().NotBeNull();
            sub.FormatString.Value.Should().Be("text");
            sub.Parameters.Should().BeNull();
        }

        [Fact]
        public void ParseShortFormAndLongFormFunctionExpressions() {

            // arrange
            var source =
@"!Base64
    Fn::Sub: text";
            var parser = new LambdaSharpParser("<literal>", source);

            // act
            parser.Start();
            var value = parser.ParseExpression();
            parser.End();

            // assert
            foreach(var message in parser.Messages) {
                _output.WriteLine(message);
            }
            parser.Messages.Any().Should().Be(false);
            value.Should().BeOfType<Base64FunctionExpression>();
            var base64 = (Base64FunctionExpression)value;
            base64.Value.Should().BeOfType<SubFunctionExpression>();
            var sub = (SubFunctionExpression)base64.Value;
            sub.FormatString.Should().NotBeNull();
            sub.FormatString.Value.Should().Be("text");
            sub.Parameters.Should().BeNull();
        }

        [Fact]
        public void ParseLongFormAndShortFormFunctionExpressions() {

            // arrange
            var source =
@"Fn::Base64:
    !Sub text";
            var parser = new LambdaSharpParser("<literal>", source);

            // act
            parser.Start();
            var value = parser.ParseExpression();
            parser.End();

            // assert
            foreach(var message in parser.Messages) {
                _output.WriteLine(message);
            }
            parser.Messages.Any().Should().Be(false);
            value.Should().BeOfType<Base64FunctionExpression>();
            var base64 = (Base64FunctionExpression)value;
            base64.Value.Should().BeOfType<SubFunctionExpression>();
            var sub = (SubFunctionExpression)base64.Value;
            sub.FormatString.Should().NotBeNull();
            sub.FormatString.Value.Should().Be("text");
            sub.Parameters.Should().BeNull();
        }

        [Fact]
        public void ParseListOfLiteralExpressions_SingleValue() {

            // arrange
            var source =
@"foo";
            var parser = new LambdaSharpParser("<literal>", source);

            // act
            parser.Start();
            var value = parser.ParseListOfLiteralExpressions();
            parser.End();

            // assert
            foreach(var message in parser.Messages) {
                _output.WriteLine(message);
            }
            parser.Messages.Any().Should().Be(false);
            value.Should().NotBeNull();
            value.Count.Should().Be(1);
            value[0].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("foo");
        }

        [Fact]
        public void ParseListOfLiteralExpressions_MultipleValues() {

            // arrange
            var source =
@"- foo
- bar";
            var parser = new LambdaSharpParser("<literal>", source);

            // act
            parser.Start();
            var value = parser.ParseListOfLiteralExpressions();
            parser.End();

            // assert
            foreach(var message in parser.Messages) {
                _output.WriteLine(message);
            }
            parser.Messages.Any().Should().Be(false);
            value.Should().NotBeNull();
            value.Count.Should().Be(2);
            value[0].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("foo");
            value[1].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("bar");
        }

        [Fact(Skip = "for debugging only")]
        // [Fact]
        public void ShowParseEvents() {

            // arrange
            var source =
@"Module: foo
Version: 1.0
Description: ""this is a description""
Items:
    - Bool: true
    - Int: 123
    - Float: 123.456
    - Func: !Sub ""${Foo}.bar""
    - Func2:
        Fn::Ref: ABC
";
             var parser = new YamlDotNet.Core.Parser(new StringReader(source));

            while(parser.MoveNext()) {
                var current = parser.Current;
                _output.WriteLine(current.ToString());
            }
        }
    }
}