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
            var module = parser.ParseDeclaration<ModuleDeclaration>();
            parser.End();

            // assert
            foreach(var message in parser.Messages) {
                _output.WriteLine(message);
            }
            parser.Messages.Any().Should().Be(false);
        }

        [Fact]
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