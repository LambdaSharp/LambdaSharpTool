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

using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using LambdaSharp.Modules;

namespace Tests.LambdaSharp.Modules.VersionInfoTests {

    public class FindLatestMatchingVersion {

        //--- Class Fields ---
        private static List<VersionInfo> _versions = new List<VersionInfo> {
            VersionInfo.Parse("0.7.0-rc4"),
            VersionInfo.Parse("0.7.0-rc5"),
            VersionInfo.Parse("0.7.0-rc6"),
            VersionInfo.Parse("0.7.0"),
            VersionInfo.Parse("0.7.1-dev"),
            VersionInfo.Parse("0.7.1"),
            VersionInfo.Parse("0.7.1.1"),
            VersionInfo.Parse("0.7.1.2"),
            VersionInfo.Parse("0.7.2"),
            VersionInfo.Parse("0.7.2.1"),
            VersionInfo.Parse("0.7.2.2-rc1"),
            VersionInfo.Parse("0.7.2.2"),
            VersionInfo.Parse("0.8.0-rc1")
        };

        //--- Constructors ---
        public FindLatestMatchingVersion(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---


        [Fact]
        public void Find_matching_prerelease_version() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.7.0-rc6");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                _versions,
                minVersion: null,
                moduleVersion => VersionInfoCompatibility.IsModuleCoreVersionCompatibleWithToolVersion(moduleVersion, toolVersion)
            );

            // assert
            result.Should().NotBeNull();
            result.CompareToVersion(VersionInfo.Parse("0.7.0-rc6")).Should().Be(0);
        }

        [Fact]
        public void Find_latest_stable_version() {

            // arrange

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                _versions,
                minVersion: null,
                validate: null
            );

            // assert
            result.Should().NotBeNull();
            result.CompareToVersion(VersionInfo.Parse("0.7.2.2")).Should().Be(0);
        }

        [Fact]
        public void Find_matching_prerelease_version_with_minimum_constraint() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.7.0-rc6");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                _versions,
                minVersion: VersionInfo.Parse("0.7.0-rc5"),
                moduleVersion => VersionInfoCompatibility.IsModuleCoreVersionCompatibleWithToolVersion(moduleVersion, toolVersion)
            );

            // assert
            result.Should().NotBeNull();
            result.CompareToVersion(VersionInfo.Parse("0.7.0-rc6")).Should().Be(0);
        }

        [Fact]
        public void Find_previous_prerelease_version_for_tool_prerelease_without_minimum_constraint() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.7.2.2-rc2");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                _versions,
                minVersion: null,
                moduleVersion => VersionInfoCompatibility.IsModuleCoreVersionCompatibleWithToolVersion(moduleVersion, toolVersion)
            );

            // assert
            result.Should().NotBeNull();
            result.CompareToVersion(VersionInfo.Parse("0.7.2.2-rc1")).Should().Be(0);
        }

        [Fact]
        public void Find_previous_stable_release_version_for_tool_prerelease_without_minimum_constraint() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.7.1-rc1");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                _versions,
                minVersion: null,
                moduleVersion => VersionInfoCompatibility.IsModuleCoreVersionCompatibleWithToolVersion(moduleVersion, toolVersion)
            );

            // assert
            result.Should().NotBeNull();
            result.CompareToVersion(VersionInfo.Parse("0.7.0")).Should().Be(0);
        }

        [Fact]
        public void Find_latest_stable_version_with_minimum_constraint() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.7.1");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                _versions,
                minVersion: null,
                moduleVersion => VersionInfoCompatibility.IsModuleCoreVersionCompatibleWithToolVersion(moduleVersion, toolVersion)
            );

            // assert
            result.Should().NotBeNull();
            result.CompareToVersion(VersionInfo.Parse("0.7.2.2")).Should().Be(0);
        }

        [Fact]
        public void Count_versions_found_for_latest_stable_version_with_minimum_constraint() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.7.1");

            // act
            var counter = 0;
            var result = VersionInfo.FindLatestMatchingVersion(
                _versions,
                minVersion: VersionInfo.Parse("0.7.1"),
                moduleVersion => {
                    ++counter;
                    return VersionInfoCompatibility.IsModuleCoreVersionCompatibleWithToolVersion(moduleVersion, toolVersion);
                }
            );

            // assert
            result.CompareToVersion(VersionInfo.Parse("0.7.2.2")).Should().Be(0);
            counter.Should().Be(2);
        }

        [Fact]
        public void Find_previous_stable_release_version_for_tool_stable_release_without_minimum_constraint() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.7.3");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                _versions,
                minVersion: null,
                moduleVersion => VersionInfoCompatibility.IsModuleCoreVersionCompatibleWithToolVersion(moduleVersion, toolVersion)
            );

            // assert
            result.Should().NotBeNull();
            result.CompareToVersion(VersionInfo.Parse("0.7.2.2")).Should().Be(0);
        }
    }
}
