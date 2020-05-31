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
using LambdaSharp.Tool.Compiler.Parser;
using LambdaSharp.Tool.Internal;
using LambdaSharp.Tool.Model;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.Compiler.Parser {
    
    public abstract class _Init {

        //--- Types ---
        public class ParserDependencyProvider : ILambdaSharpParserDependencyProvider {

            //--- Fields ---
            private readonly List<string> _messages;

            //--- Constructors ---
            public ParserDependencyProvider(List<string> messages) => _messages = messages ?? throw new ArgumentNullException(nameof(messages));

            //--- Properties ---
            public IEnumerable<string> Messages => _messages;
            public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();

            //--- Methods ---
            public void Log(Error error, SourceLocation sourceLocation)
                => _messages.Add($"ERROR{error.Code}: {error.Message} @ {sourceLocation?.FilePath ?? "<n/a>"}({sourceLocation?.LineNumberStart ?? 0},{sourceLocation?.ColumnNumberStart ?? 0})");

            public string ReadFile(string filePath) => Files[filePath];
        }

        public class BuilderDependencyProvider : IBuilderDependencyProvider {

            //--- Fields ---
            private readonly List<string> _messages;

            //--- Constructors ---
            public BuilderDependencyProvider(List<string> messages) => _messages = messages ?? throw new ArgumentNullException(nameof(messages));

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
                var label = entry.Severity.ToString().ToUpperInvariant();
                if(sourceLocation == null) {
                    _messages.Add($"{label}{((entry.Code != 0) ? $" ({entry.Code})" : "")}: {entry.Message}");
                } else if(exact) {
                    _messages.Add($"{label}{((entry.Code != 0) ? $" ({entry.Code})" : "")}: {entry.Message} @ {sourceLocation}");
                } else {
                    _messages.Add($"{label}{((entry.Code != 0) ? $" ({entry.Code})" : "")}: {entry.Message} @ (near) {sourceLocation}");
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
        protected readonly ParserDependencyProvider Provider;
        protected readonly List<string> Messages = new List<string>();

        //--- Constructors ---
        public _Init(ITestOutputHelper output) {
            Output = output;
            Provider = new ParserDependencyProvider(Messages);
        }

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
}