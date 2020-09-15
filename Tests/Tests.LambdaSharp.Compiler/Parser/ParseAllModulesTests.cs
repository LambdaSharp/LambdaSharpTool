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
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.Parser {

    public class ParseAllModulesTests : _Init {

        //--- Constructors ---
        public ParseAllModulesTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void Test() {

            // arrange
            var lambdaSharpPath = Environment.GetEnvironmentVariable("LAMBDASHARP");
            ShouldNotBeNull(lambdaSharpPath, "LAMBDASHARP environment variable is missing");
            Provider.FindFile = filePath => File.Exists(filePath)
                ? File.ReadAllText(filePath)
                : (string?)null;

            // act

            // enumerate all 'Module.yml' files in LambdaSharp folder and all YAMl files in the test folder
            foreach(var modulePath in Directory.GetFiles(lambdaSharpPath, "Module.yml", SearchOption.AllDirectories)
                .Union(Directory.GetFiles(Path.Combine(lambdaSharpPath, "Tests", "Modules"), "*.yml", SearchOption.TopDirectoryOnly))
                .Distinct()
                .ToArray()
            ) {
                try {
                    Reset();
                    NewParser(lambdaSharpPath, Path.GetRelativePath(lambdaSharpPath, modulePath)).ParseModule();
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