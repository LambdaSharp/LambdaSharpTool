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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LambdaSharp.Compiler;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Parser;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.TypeSystem;
using LambdaSharp.Compiler.Validators;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler {

    public abstract class _Init : IModuleValidatorDependencyProvider {

        //--- Types ---
        public class InMemoryLogger : ILogger {

            //--- Fields ---
            private readonly List<string> _messages;

            //--- Constructors ---
            public InMemoryLogger(List<string> messages) => _messages = messages ?? throw new ArgumentNullException(nameof(messages));

            //--- Properties ---
            public IEnumerable<string> Messages => _messages;

            //--- Methods ---
            public void Log(IBuildReportEntry entry, SourceLocation? sourceLocation, bool exact) {
                var column = ((sourceLocation?.ColumnNumberStart ?? 0) > 0)
                    ? $", column {sourceLocation?.ColumnNumberStart}"
                    : "";
                var line = ((sourceLocation?.LineNumberStart ?? 0) > 0)
                    ? $": line {sourceLocation?.LineNumberStart}{column}"
                    : "";
                var position = (sourceLocation?.FilePath != null)
                    ? $" in {sourceLocation?.FilePath}{line}"
                    : "";
                var code = (entry.Code != 0)
                    ? entry.Code.ToString()
                    : "";
                _messages.Add($"{entry.Severity.ToString().ToUpperInvariant()}{code}: {entry.Message}{position}");
            }
        }

        public class ParserDependencyProvider : ILambdaSharpParserDependencyProvider {

            //--- Fields ---
            private readonly InMemoryLogger _logger;

            //--- Constructors ---
            public ParserDependencyProvider(List<string> messages) => _logger = new InMemoryLogger(messages ?? throw new ArgumentNullException(nameof(messages)));

            //--- Properties ---
            public IEnumerable<string> Messages => _logger.Messages;
            public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();
            public ILogger Logger => _logger;

            //--- Methods ---
            public string ReadFile(string filePath) => Files[filePath];
        }

        // TODO: reinstate or delete
        // public class BuilderDependencyProvider : IBuilderDependencyProvider {

        //     //--- Fields ---
        //     private readonly List<string> _messages;

        //     //--- Constructors ---
        //     public BuilderDependencyProvider(List<string> messages) => _messages = messages ?? throw new ArgumentNullException(nameof(messages));

        //     //--- Properties ---
        //     public string ToolDataDirectory => Path.Combine(Environment.GetEnvironmentVariable("LAMBDASHARP") ?? throw new ApplicationException("missing LAMBDASHARP environment variable"), "Tests", "Tests.LambdaSharp.Tool-Test-Output");
        //     public IEnumerable<string> Messages => _messages;

        //     //--- Methods ---
        //     public async Task<string> GetS3ObjectContentsAsync(string bucketName, string key) {
        //         switch(bucketName) {
        //         case "lambdasharp":
        //             return GetType().Assembly.ReadManifestResource($"Resources/{key}");
        //         default:

        //             // nothing to do
        //             break;
        //         }
        //         return null;
        //     }

        //     public async Task<IEnumerable<string>> ListS3BucketObjects(string bucketName, string prefix) {
        //         switch(bucketName) {
        //         case "lambdasharp":
        //             switch(prefix) {
        //             case "lambdasharp/LambdaSharp/Core/":
        //                 return new[] {
        //                     "0.7.0"
        //                 };
        //             case "lambdasharp/LambdaSharp/S3.Subscriber/":
        //                 return new[] {
        //                     "0.7.3"
        //                 };
        //             default:

        //                 // nothing to do
        //                 break;
        //             }
        //             break;
        //         default:

        //             // nothing to do
        //             break;
        //         }
        //         return Enumerable.Empty<string>();
        //     }

        //     public void Log(IBuildReportEntry entry, SourceLocation sourceLocation, bool exact) {
        //         var label = entry.Severity.ToString().ToUpperInvariant();
        //         if(sourceLocation == null) {
        //             _messages.Add($"{label}{((entry.Code != 0) ? $" ({entry.Code})" : "")}: {entry.Message}");
        //         } else if(exact) {
        //             _messages.Add($"{label}{((entry.Code != 0) ? $" ({entry.Code})" : "")}: {entry.Message} @ {sourceLocation}");
        //         } else {
        //             _messages.Add($"{label}{((entry.Code != 0) ? $" ({entry.Code})" : "")}: {entry.Message} @ (near) {sourceLocation}");
        //         }
        //     }

        //     public async Task<CloudFormationSpec> ReadCloudFormationSpecAsync(RegionEndpoint region, VersionInfo version) {
        //         var assembly = GetType().Assembly;
        //         using(var specResource = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.CloudFormationResourceSpecification.json.gz"))
        //         using(var specGzipStream = new GZipStream(specResource, CompressionMode.Decompress))
        //         using(var specReader = new StreamReader(specGzipStream)) {
        //             return JsonConvert.DeserializeObject<CloudFormationSpec>(specReader.ReadToEnd());
        //         }
        //     }
        // }

        //--- Class Methods ---
        protected static void ShouldNotBeNull([NotNull] object? value) {
            value.Should().NotBeNull();
            if(value == null) {
                throw new ShouldNeverHappenException();
            }
        }

        //--- Fields ---
        protected readonly ITestOutputHelper Output;
        protected readonly ParserDependencyProvider Provider;
        protected readonly List<string> Messages = new List<string>();
        protected readonly Dictionary<string, AItemDeclaration> Declarations = new Dictionary<string, AItemDeclaration>();

        //--- Constructors ---
        public _Init(ITestOutputHelper output) {
            Output = output;
            Provider = new ParserDependencyProvider(Messages);
            Logger = new InMemoryLogger(Messages);
        }

        //--- Properties ---
        public ILogger Logger { get; }

        //--- Methods ---
        protected void AddSource(string filePath, string source) => Provider.Files.Add(filePath, source);

        protected LambdaSharpParser NewParser(string source) {
            if(source.StartsWith("@", StringComparison.Ordinal)) {
                source = ResourceReader.ReadText(source.Substring(1));
            }
            AddSource("Module.yml", source);
            return new LambdaSharpParser(Provider, "Module.yml");
        }

        protected LambdaSharpParser NewParser(string workdingDirectory, string filename) {
            return new LambdaSharpParser(Provider, workdingDirectory, filename);
        }

        protected void ExpectedMessages(params string[] expected) {
            var unexpected = Provider.Messages.Where(message => !expected.Contains(message)).ToList();
            var missing = expected.Where(message => !Provider.Messages.Contains(message)).ToList();
            foreach(var message in unexpected) {
                Output.WriteLine(message);
            }
            foreach(var message in missing) {
                Output.WriteLine("MISSING MESSAGE: " + message);
            }
            unexpected.Any().Should().Be(false);
            missing.Any().Should().Be(false);
        }

        //--- IModuleValidatorDependencyProvider Members ---
        bool IModuleValidatorDependencyProvider.TryGetResourceType(string typeName, out IResourceType resourceType) => throw new NotImplementedException();
        Task<string> IModuleValidatorDependencyProvider.ConvertKmsAliasToArn(string alias) => throw new NotImplementedException();

        void IModuleValidatorDependencyProvider.DeclareItem(AItemDeclaration declaration)
            => Declarations.Add(declaration.FullName, declaration);

        bool IModuleValidatorDependencyProvider.TryGetItem(string fullname, [NotNullWhen(true)] out AItemDeclaration? itemDeclaration)
            => Declarations.TryGetValue(fullname, out itemDeclaration);
    }
}