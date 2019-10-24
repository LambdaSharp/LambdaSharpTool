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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using LambdaSharp.Tool.Parser;
using LambdaSharp.Tool.Parser.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Parser {

    public class ParseTests {

        //--- Types ---
        public class DependencyProvider : ILambdaSharpParserDependencyProvider {

            //--- Properties ---
            public List<string> Messages { get; private set; } = new List<string>();
            public Dictionary<string, string> Files { get; private set; } = new Dictionary<string, string>();

            //--- Methods ---
            public void LogError(string filePath, int line, int column, string message)
                => Messages.Add($"ERROR: {message} @ {filePath}({line},{column})");

            public string ReadFile(string filePath) => Files[filePath];
        }

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
            var provider = new DependencyProvider {
                Files = {
                    ["test.yml"] = source
                }
            };
            var parser = new LambdaSharpParser(provider, "test.yml");

            // act
            var module = parser.ParseDeclarationOf<ModuleDeclaration>();

            // assert
            foreach(var message in provider.Messages) {
                _output.WriteLine(message);
            }
            provider.Messages.Any().Should().Be(false);
        }

        [Fact]
        public void ParseLiteralExpression() {

            // arrange
            var source =
@"text";
            var provider = new DependencyProvider {
                Files = {
                    ["test.yml"] = source
                }
            };
            var parser = new LambdaSharpParser(provider, "test.yml");

            // act
            var value = parser.ParseExpression();

            // assert
            foreach(var message in provider.Messages) {
                _output.WriteLine(message);
            }
            provider.Messages.Any().Should().Be(false);
            value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("text");
        }


        [Fact]
        public void ParseSubFunctionExpression() {

            // arrange
            var source =
@"!Sub text";
            var provider = new DependencyProvider {
                Files = {
                    ["test.yml"] = source
                }
            };
            var parser = new LambdaSharpParser(provider, "test.yml");

            // act
            var value = parser.ParseExpression();

            // assert
            foreach(var message in provider.Messages) {
                _output.WriteLine(message);
            }
            provider.Messages.Any().Should().Be(false);
            var sub = value.Should().BeOfType<SubFunctionExpression>().Which;
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
            var provider = new DependencyProvider {
                Files = {
                    ["test.yml"] = source
                }
            };
            var parser = new LambdaSharpParser(provider, "test.yml");

            // act
            var value = parser.ParseExpression();

            // assert
            foreach(var message in provider.Messages) {
                _output.WriteLine(message);
            }
            provider.Messages.Any().Should().Be(false);
            var base64 = value.Should().BeOfType<Base64FunctionExpression>().Which;
            var sub = base64.Value.Should().BeOfType<SubFunctionExpression>().Which;
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
            var provider = new DependencyProvider {
                Files = {
                    ["test.yml"] = source
                }
            };
            var parser = new LambdaSharpParser(provider, "test.yml");

            // act
            var value = parser.ParseExpression();

            // assert
            foreach(var message in provider.Messages) {
                _output.WriteLine(message);
            }
            provider.Messages.Any().Should().Be(false);
            var base64 = value.Should().BeOfType<Base64FunctionExpression>().Which;
            var sub = base64.Value.Should().BeOfType<SubFunctionExpression>().Which;
            sub.FormatString.Should().NotBeNull();
            sub.FormatString.Value.Should().Be("text");
            sub.Parameters.Should().BeNull();
        }

        [Fact]
        public void ParseListOfLiteralExpressions_SingleValue() {

            // arrange
            var source =
@"foo";
            var provider = new DependencyProvider {
                Files = {
                    ["test.yml"] = source
                }
            };
            var parser = new LambdaSharpParser(provider, "test.yml");

            // act
            var value = parser.ParseListOfLiteralExpressions();

            // assert
            foreach(var message in provider.Messages) {
                _output.WriteLine(message);
            }
            provider.Messages.Any().Should().Be(false);
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
            var provider = new DependencyProvider {
                Files = {
                    ["test.yml"] = source
                }
            };
            var parser = new LambdaSharpParser(provider, "test.yml");

            // act
            var value = parser.ParseListOfLiteralExpressions();

            // assert
            foreach(var message in provider.Messages) {
                _output.WriteLine(message);
            }
            provider.Messages.Any().Should().Be(false);
            value.Should().NotBeNull();
            value.Count.Should().Be(2);
            value[0].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("foo");
            value[1].Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("bar");
        }

        [Fact]
        public void ParseNestedIncludes() {

            // arrange
            var provider = new DependencyProvider {
                Files = {
                    ["test.yml"] = "!Include include.yml",
                    ["include.yml"] = "!Include include.txt",
                    ["include.txt"] = "hello world!"
                }
            };
            var parser = new LambdaSharpParser(provider, "test.yml");

            // act
            var value = parser.ParseExpression();

            // assert
            foreach(var message in provider.Messages) {
                _output.WriteLine(message);
            }
            provider.Messages.Any().Should().Be(false);
            value.Should().NotBeNull();
            var literal = value.Should().BeOfType<LiteralExpression>().Which;
            literal.Value.Should().Be("hello world!");
            literal.SourceLocation.FilePath.Should().Be("include.txt");
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