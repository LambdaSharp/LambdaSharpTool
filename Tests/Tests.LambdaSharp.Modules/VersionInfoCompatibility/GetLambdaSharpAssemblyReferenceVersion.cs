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

    public class GetLambdaSharpAssemblyReferenceVersion {

        //--- Constructors ---
        public GetLambdaSharpAssemblyReferenceVersion(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Get_compatible_assembly_version_for_fractional_major_tool_version_0_8_0() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.8.0.8");

            // act
            var assemblyVersion = VersionInfoCompatibility.GetLambdaSharpAssemblyReferenceVersion(toolVersion);

            // assert
            assemblyVersion.Should().NotBeNull();
            assemblyVersion.ToString().Should().Be("0.8.0");
        }

        [Fact]
        public void Get_compatible_assembly_version_for_fractional_major_tool_version_0_8_1() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.8.1.0");

            // act
            var assemblyVersion = VersionInfoCompatibility.GetLambdaSharpAssemblyReferenceVersion(toolVersion);

            // assert
            assemblyVersion.Should().NotBeNull();
            assemblyVersion.ToString().Should().Be("0.8.1");
        }

        [Fact]
        public void Get_compatible_assembly_version_for_integral_major_tool_version() {

            // arrange
            var toolVersion = VersionInfo.Parse("1.2.3");

            // act
            var assemblyVersion = VersionInfoCompatibility.GetLambdaSharpAssemblyReferenceVersion(toolVersion);

            // assert
            assemblyVersion.Should().NotBeNull();
            assemblyVersion.ToString().Should().Be("1.2");
        }
    }
}
