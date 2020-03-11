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

#nullable disable

using System;
using System.IO;
using System.Linq;
using LambdaSharp.Tool;
using LambdaSharp.Tool.Compiler;
using LambdaSharp.Tool.Compiler.Analyzers;
using LambdaSharp.Tool.Compiler.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Compiler.Parser {

    public class ParseTests : _Init {

        //--- Constructors ---
        public ParseTests(ITestOutputHelper output) : base(output) {

            // NOTE (2020-03-09, bjorg): the LAMBDASHARP_VERSION environment variable is used to
            //  avoid hard-coding the tool version into the tests.
            if(Environment.GetEnvironmentVariable("LAMBDASHARP_VERSION") == null) {
                throw new ApplicationException("LAMBDASHARP_VERSION environment variable not initialized");
            }
        }

        //--- Methods ---

        [Fact]
        public void AnalyzeRef() {

            // arrange
            var parser = NewParser(
@"Module: My.Module
Items:
    - Variable: BarVariable
      Value: !Ref FooResource
    - Resource: FooResource
      Type: AWS::SNS::Topic
");
            var moduleDeclaration = parser.ParseSyntaxOfType<ModuleDeclaration>();

            // act
            var builder = new Builder(new BuilderDependencyProvider(Messages));
            builder.ToolVersion = VersionInfo.Parse(Environment.GetEnvironmentVariable("LAMBDASHARP_VERSION"));
            moduleDeclaration = moduleDeclaration.Visit(new DiscoverDependenciesAnalyzer(builder));
            moduleDeclaration = moduleDeclaration.Visit(new StructureAnalyzer(builder));
            moduleDeclaration = moduleDeclaration.Visit(new LinkReferencesAnalyzer(builder));

// TODO: debugging only
foreach(var item in builder.ItemDeclarations.OrderBy(item => item.FullName)) {
    builder.Log(new Debug($"{item.FullName} -> {item.GetType().Name} @ {item.SourceLocation}"));
}

            builder.DetectCircularDependencies();
            new ResolveReferences(builder).Resolve(moduleDeclaration);

            // assert
            ExpectNoMessages();
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
    - Int: 0123
    - Float: 123.456
    - String: |
        This entire block of text will be the value of the 'literal_block' key,
        with line breaks being preserved.

        The literal continues until de-dented, and the leading indentation is
        stripped.

            Any lines that are 'more-indented' keep the rest of their indentation -
            these lines will be indented by 4 spaces.
    - String: >
        This entire block of text will be the value of 'folded_style', but this
        time, all newlines will be replaced with a single space.

        Blank lines, like above, are converted to a newline character.

            'More-indented' lines keep their newlines, too -
            this text will appear over two lines.
    - String: 'a string with '' a single quote'
    - String: ""a string with an escaped charater: \u00B2""
    - Func: !Sub ""${Foo}.bar""
    - Func2:
        Fn::Ref: ABC
";
            var parser = new YamlDotNet.Core.Parser(new StringReader(source));

            while(parser.MoveNext()) {
                var current = parser.Current;
                Output.WriteLine($"{current.ToString()} ({current.Start.Line}, {current.Start.Column})");
            }
        }
    }
}