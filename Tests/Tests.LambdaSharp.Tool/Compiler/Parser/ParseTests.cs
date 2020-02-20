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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using FluentAssertions;
using LambdaSharp.Tool;
using LambdaSharp.Tool.Compiler;
using LambdaSharp.Tool.Compiler.Analyzers;
using LambdaSharp.Tool.Compiler.Parser;
using LambdaSharp.Tool.Compiler.Parser.Syntax;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Compiler.Parser {

    public abstract class _Init {

        //--- Types ---
        public class ParserDependencyProvider : ILambdaSharpParserDependencyProvider {

            //--- Properties ---
            public List<string> Messages { get; private set; } = new List<string>();
            public Dictionary<string, string> Files { get; private set; } = new Dictionary<string, string>();

            //--- Methods ---
            public void Log(Error error, SourceLocation sourceLocation)
                => Messages.Add($"ERROR{error.Code}: {error.Message} @ {sourceLocation?.FilePath ?? "<n/a>"}({sourceLocation?.LineNumberStart ?? 0},{sourceLocation?.ColumnNumberStart ?? 0})");

            public string ReadFile(string filePath) => Files[filePath];
        }

        public class BuilderDependencyProvider : IBuilderDependencyProvider {

            //--- Fields ---
            private readonly List<string> _messages = new List<string>();

            //--- Properties ---
            public string ToolDataDirectory => Path.Combine(Environment.GetEnvironmentVariable("LAMBDASHARP") ?? throw new ApplicationException("missing LAMBDASHARP environment variable"), "Tests", "Tests.LambdaSharp.Tool-Test-Output");
            public IEnumerable<string> Messages => _messages;

            //--- Methods ---
            public async Task<string> GetS3ObjectContentsAsync(string bucketName, string key) {
                switch(bucketName) {
                case "lambdasharp":
                    return GetType().Assembly.ReadManifestResource($"Resources/{key}");
                default:

                    // nothing to do
                    break;
                }
                return null;
            }

            public async Task<IEnumerable<string>> ListS3BucketObjects(string bucketName, string prefix) {
                switch(bucketName) {
                case "lambdasharp":
                    switch(prefix) {
                    case "lambdasharp/LambdaSharp/Core/":
                        return new[] {
                            "0.7.0"
                        };
                    case "lambdasharp/LambdaSharp/S3.Subscriber/":
                        return new[] {
                            "0.7.3"
                        };
                    default:

                        // nothing to do
                        break;
                    }
                    break;
                default:

                    // nothing to do
                    break;
                }
                return Enumerable.Empty<string>();
            }

            public void Log(IBuildReportEntry entry, SourceLocation sourceLocation, bool exact) {

                // TODO: message should not be captured as strings, which makes further formatting impossible (such as colorization)
                var label = entry.Severity.ToString().ToUpperInvariant();
                if(sourceLocation == null) {
                    _messages.Add($"{label}{entry.Code}: {entry.Message}");
                } else if(exact) {
                    _messages.Add($"{label}{entry.Code}: {entry.Message} @ {sourceLocation.FilePath ?? "n/a"}({sourceLocation.LineNumberStart},{sourceLocation.ColumnNumberStart})");
                } else {
                    _messages.Add($"{label}{entry.Code}: {entry.Message} @ (near) {sourceLocation.FilePath ?? "n/a"}({sourceLocation.LineNumberStart},{sourceLocation.ColumnNumberStart})");
                }
            }

            public async Task<CloudFormationSpec> ReadCloudFormationSpecAsync(RegionEndpoint region, VersionInfo version) {
                var assembly = GetType().Assembly;
                using(var specResource = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.CloudFormationResourceSpecification.json.gz"))
                using(var specGzipStream = new GZipStream(specResource, CompressionMode.Decompress))
                using(var specReader = new StreamReader(specGzipStream)) {
                    return JsonConvert.DeserializeObject<CloudFormationSpec>(specReader.ReadToEnd());
                }
            }
        }

        //--- Fields ---
        protected readonly ITestOutputHelper Output;
        protected readonly ParserDependencyProvider Provider = new ParserDependencyProvider();

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
            var moduleDeclaration = parser.ParseSyntaxOfType<ModuleDeclaration>();

            // act
            var builder = new Builder(new BuilderDependencyProvider());
            moduleDeclaration.Visit(parent: null, new DiscoverDependenciesAnalyzer(builder));
            moduleDeclaration.Visit(parent: null, new StructureAnalyzer(builder));
            moduleDeclaration.Visit(parent: null, new LinkReferencesAnalyzer(builder));
            new ReferenceResolver(builder).Visit();

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
                Output.WriteLine(current.ToString());
            }
        }
    }
}