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
using LambdaSharp.Compiler.SyntaxProcessors;
using Xunit.Abstractions;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Modules;
using System.IO;
using LambdaSharp.CloudFormation.TypeSystem;
using LambdaSharp.Modules.Metadata;

namespace Tests.LambdaSharp.Compiler {

    public abstract class _Init : ISyntaxProcessorDependencyProvider {

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
            public Func<string, string?>? FindFile;
            private readonly InMemoryLogger _logger;

            //--- Constructors ---
            public ParserDependencyProvider(List<string> messages) => _logger = new InMemoryLogger(messages ?? throw new ArgumentNullException(nameof(messages)));

            //--- Properties ---
            public IEnumerable<string> Messages => _logger.Messages;
            public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();
            public ILogger Logger => _logger;

            //--- Methods ---
            public string ReadFile(string filePath) {
                if(Files.TryGetValue(filePath, out var contents)) {
                    return contents;
                }
                return FindFile?.Invoke(filePath) ?? throw new FileNotFoundException(filePath);
            }
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
        protected static void ShouldNotBeNull([NotNull] object? value, string? because = null) {
            value.Should().NotBeNull(because);
            if(value == null) {
                throw new ShouldNeverHappenException();
            }
        }

        //--- Fields ---
        protected readonly ITestOutputHelper Output;
        protected readonly ParserDependencyProvider Provider;
        protected readonly List<string> Messages = new List<string>();
        protected readonly Dictionary<string, AItemDeclaration> Declarations = new Dictionary<string, AItemDeclaration>();
        protected readonly Dictionary<string, AExpression> ReferenceExpressions = new Dictionary<string, AExpression>();
        protected readonly Dictionary<string, AExpression> ValueExpressions = new Dictionary<string, AExpression>();

        //--- Constructors ---
        public _Init(ITestOutputHelper output) {
            Output = output;
            Provider = new ParserDependencyProvider(Messages);
            Logger = new InMemoryLogger(Messages);
        }

        //--- Properties ---
        public ILogger Logger { get; }

        //--- Methods ---
        protected void Reset() {
            Provider.Files.Clear();
        }

        protected void AddSource(string filePath, string source) => Provider.Files.Add(filePath, source);

        protected LambdaSharpParser NewParser(string source) {
            if(source.StartsWith("@", StringComparison.Ordinal)) {
                source = ResourceReader.ReadText(source.Substring(1));
            }
            AddSource("Module.yml", source);
            return new LambdaSharpParser(Provider, "Module.yml");
        }

        protected LambdaSharpParser NewParser(string workingDirectory, string filename) {
            return new LambdaSharpParser(Provider, workingDirectory, filename);
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

        //--- IModuleProcessorDependencyProvider Members ---
        IEnumerable<AItemDeclaration> ISyntaxProcessorDependencyProvider.Declarations => Declarations.Values;

        ILogger ISyntaxProcessorDependencyProvider.Logger => Logger;

        bool ISyntaxProcessorDependencyProvider.TryGetResourceType(string typeName, [NotNullWhen(true)] out IResourceType? resourceType)
            => throw new NotImplementedException();

        Task<string> ISyntaxProcessorDependencyProvider.ConvertKmsAliasToArn(string alias)
            => throw new NotImplementedException();

        void ISyntaxProcessorDependencyProvider.DeclareItem(ASyntaxNode? parent, AItemDeclaration itemDeclaration) {
            if(itemDeclaration == null) {
                throw new ArgumentNullException(nameof(itemDeclaration));
            }
            if(parent == null) {
                throw new ArgumentNullException(nameof(parent));
            }
            parent.Adopt(itemDeclaration);
            Declarations.Add(itemDeclaration.FullName, itemDeclaration);
        }

        bool ISyntaxProcessorDependencyProvider.TryGetItem(string fullname, [NotNullWhen(true)] out AItemDeclaration? itemDeclaration)
            => Declarations.TryGetValue(fullname, out itemDeclaration);

        Task<ModuleManifest> ISyntaxProcessorDependencyProvider.ResolveModuleInfoAsync(ModuleManifestDependencyType dependencyType, ModuleInfo moduleInfo)
            => throw new NotImplementedException();

        void ISyntaxProcessorDependencyProvider.DeclareReferenceExpression(string fullname, AExpression expression)
            => ReferenceExpressions[fullname] = expression;

        void ISyntaxProcessorDependencyProvider.DeclareValueExpression(string fullname, AExpression expression)
            => ValueExpressions[fullname] = expression;

        bool ISyntaxProcessorDependencyProvider.TryGetReferenceExpression(string fullname, [NotNullWhen(true)] out AExpression? expression)
            => ReferenceExpressions.TryGetValue(fullname, out expression);

        bool ISyntaxProcessorDependencyProvider.TryGetValueExpression(string fullname, [NotNullWhen(true)] out AExpression? expression)
            => ValueExpressions.TryGetValue(fullname, out expression);
    }
}