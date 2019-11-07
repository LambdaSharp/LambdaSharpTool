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

using LambdaSharp.Tool.Compiler.Parser.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Compiler.Parser {

    public class ParseSyntaxOfTests : _Init {

        //--- Constructors ---
        public ParseSyntaxOfTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void ParseAllFields() {

            // arrange
            var parser = NewParser(
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
");

            // act
            var module = parser.ParseSyntaxOfType<ModuleDeclaration>();

            // assert
            ExpectNoMessages();
        }
    }
}