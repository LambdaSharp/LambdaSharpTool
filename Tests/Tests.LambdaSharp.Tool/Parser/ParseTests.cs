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
using LambdaSharp.Tool.Parser.Analyzers;
using LambdaSharp.Tool.Parser.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Parser {

    public abstract class _Init {

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
        protected readonly ITestOutputHelper Output;
        protected readonly DependencyProvider Provider = new DependencyProvider();

        //--- Constructors ---
        public _Init(ITestOutputHelper output) => Output = output;

        //--- Methods ---
        protected void AddSource(string filePath, string source) => Provider.Files.Add(filePath, source);

        protected LambdaSharpParser NewParser(string source) {
            AddSource("test.yml", source);
            return new LambdaSharpParser(Provider, "test.yml");
        }

        protected void ExpectNoMessages() {
            foreach(var message in Provider.Messages) {
                Output.WriteLine(message);
            }
            Provider.Messages.Any().Should().Be(false);
        }
    }

    public class ParseTests : _Init {

        //--- Constructors ---
        public ParseTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void AnalyzeRef() {

            // arrange
            var parser = NewParser(
@"Module: My.Module
Items:
    - Resource: FooResource
      Type: AWS::SNS::Topic
    - Variable: BarVariable
      Value: !Ref FooResource
");
            var moduleDeclaration = parser.ParseSyntaxOf<ModuleDeclaration>();

            // act
            var builder = new Builder();
            moduleDeclaration.Visit(parent: null, new SyntaxHierarchyAnalyzer(builder));
            moduleDeclaration.Visit(parent: null, new DeclarationsVisitor(builder));
            moduleDeclaration.Visit(parent: null, new ReferencesAnalyzer(builder));

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
    - Int: 123
    - Float: 123.456
    - Func: !Sub ""${Foo}.bar""
    - Func2:
        Fn::Ref: ABC
";
            var parser = new YamlDotNet.Core.Parser(new StringReader(source));

            while(parser.MoveNext()) {
                var current = parser.Current;
                Output.WriteLine(current.ToString());
            }
        }
    }
}