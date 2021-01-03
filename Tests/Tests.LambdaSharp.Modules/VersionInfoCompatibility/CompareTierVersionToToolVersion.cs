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
using Xunit.Abstractions;
using LambdaSharp.Modules;
using Xunit;

namespace Tests.LambdaSharp.Modules.VersionInfoCompatibilityTests {

    public class CompareTierVersionToToolVersion {

        //--- Constructors ---
        public CompareTierVersionToToolVersion(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Tool_prerelease_version_is_newer_than_previous_tier_stable_version() {

            // arrange
            var tierVersion = VersionInfo.Parse("0.8.0.9");
            var toolVersion = VersionInfo.Parse("0.8.1.0-rc1");

            // act
            var result = VersionInfoCompatibility.CompareTierVersionToToolVersion(tierVersion, toolVersion);

            // assert
            result.Should().Be(-1);
        }

        [Fact]
        public void Tool_stable_release_version_is_newer_than_previous_tier_prerelease_version() {

            // arrange
            var tierVersion = VersionInfo.Parse("0.8.0.9-rc1");
            var toolVersion = VersionInfo.Parse("0.8.1.0");

            // act
            var result = VersionInfoCompatibility.CompareTierVersionToToolVersion(tierVersion, toolVersion);

            // assert
            result.Should().Be(-1);
        }
    }
}
