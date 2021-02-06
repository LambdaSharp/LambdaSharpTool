/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using Xunit;
using Xunit.Abstractions;
using LambdaSharp.Modules;

namespace Tests.LambdaSharp.Modules.VersionInfoCompatibilityTests {

    public class IsValidLambdaSharpAssemblyReferenceForToolVersion {

        //--- Constructors ---
        public IsValidLambdaSharpAssemblyReferenceForToolVersion(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void IsValidLambdaSharpAssemblyReferenceForToolVersionForNetCore21() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.8.1.2");
            var framework = "netcoreapp2.1";
            var lambdaSharpAssemblyVersion = "0.8.1.*";

            // act
            var result = VersionInfoCompatibility.IsValidLambdaSharpAssemblyReferenceForToolVersion(toolVersion, framework, lambdaSharpAssemblyVersion, out var outdated);

            // assert
            result.Should().Be(true);
            outdated.Should().Be(false);
        }

        [Fact]
        public void IsValidLambdaSharpAssemblyReferenceForToolVersionForNetCore31() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.8.1.2");
            var framework = "netcoreapp3.1";
            var lambdaSharpAssemblyVersion = "0.8.1.*";

            // act
            var result = VersionInfoCompatibility.IsValidLambdaSharpAssemblyReferenceForToolVersion(toolVersion, framework, lambdaSharpAssemblyVersion, out var outdated);

            // assert
            result.Should().Be(true);
            outdated.Should().Be(false);
        }
    }
}
