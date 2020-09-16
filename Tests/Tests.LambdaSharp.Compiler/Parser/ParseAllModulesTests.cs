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
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.Parser {

    public class ParseAllModulesTests : _Init {

        //--- Fields ---
        private readonly string _lambdaSharpPath;

        //--- Constructors ---
        public ParseAllModulesTests(ITestOutputHelper output) : base(output) {
            var lambdaSharpPath = Environment.GetEnvironmentVariable("LAMBDASHARP");
            ShouldNotBeNull(lambdaSharpPath, "LAMBDASHARP environment variable is missing");
            _lambdaSharpPath = lambdaSharpPath;
        }

        //--- Methods ---

        [Fact]
        public void ParseSampleModules( ) => ParseModules(Directory.GetFiles(Path.Combine(_lambdaSharpPath, "Samples"), "Module.yml", SearchOption.AllDirectories));

        [Fact]
        public void ParseAllTestModules( ) => ParseModules(Directory.GetFiles(Path.Combine(_lambdaSharpPath, "Tests", "Modules"), "*.yml", SearchOption.TopDirectoryOnly));

        [Fact]
        public void ParseDemoModules( ) => ParseModules(Directory.GetFiles(Path.Combine(_lambdaSharpPath, "Demos"), "Module.yml", SearchOption.AllDirectories));

        [Fact]
        public void ParseLambdaSharpModules( ) => ParseModules(Directory.GetFiles(Path.Combine(_lambdaSharpPath, "Modules"), "Module.yml", SearchOption.AllDirectories));

        private void ParseModules(IEnumerable<string> modulePaths) {
            modulePaths.Any().Should().BeTrue("modules found");

            // arrange
            Provider.FindFile = filePath => File.Exists(filePath)
                ? File.ReadAllText(filePath)
                : (string?)null;

            // act
            foreach(var modulePath in modulePaths) {
                try {
                    Reset();
                    NewParser(_lambdaSharpPath, Path.GetRelativePath(_lambdaSharpPath, modulePath)).ParseModule();
                    ExpectedMessages();
                } catch {
                    Output.WriteLine($"FAILED MODULE: {modulePath}");
                    throw;
                }
            }

            // assert
        }
    }
}