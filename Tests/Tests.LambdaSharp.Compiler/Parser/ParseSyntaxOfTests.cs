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
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Compiler.SyntaxProcessors;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.Parser {

    // TODO: add CloudWatch event source
    // TODO: add package Build test
    // TODO: add tests to recover from badly formed YAML

    public class ParseSyntaxOfTests : _Init {

        //--- Constructors ---
        public ParseSyntaxOfTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void ParseAllFields() {

            // arrange
            var parser = NewParser("@Parser/ParseSyntaxOfTests/AllDeclarationsModule.yml");

            // act
            var module = parser.ParseModule();

            // assert
            ExpectedMessages();
            module.Should().NotBeNull();
            new SyntaxTreeIntegrityProcessor(this).ValidateIntegrity(module!);
        }


        [Fact]
        public void ParseDecryptSecretFunction() {

            // arrange
            var parser = NewParser("LambdaSharp.Compiler.dll", "DecryptSecretFunction.js");

            // act
            var expression = parser.ParseExpression();

            // assert
            ExpectedMessages();
            expression.Should().NotBeNull();
            expression.Should().BeOfType<LiteralExpression>()
              .Which.Value.Should().StartWith("const AWS = require('aws-sdk');");
        }

        [Fact]
        public void ParseStandardModule() {

            // arrange
            var parser = NewParser("LambdaSharp.Compiler.dll", "Standard-Module.yml");

            // act
            var module = parser.ParseModule();

            // assert
            ExpectedMessages();
            module.Should().NotBeNull();
        }

        [Fact]
        public void ParseLambdaSharpModule() {

            // arrange
            var parser = NewParser("LambdaSharp.Compiler.dll", "LambdaSharp-Module.yml");

            // act
            var module = parser.ParseModule();

            // assert
            ExpectedMessages();
            module.Should().NotBeNull();
        }
    }
}