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

namespace Tests.LambdaSharp.Modules.VersionInfoTests {

    public class CompareTo {

        //--- Constructors ---
        public CompareTo(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Compare_stable_versions() {
            IsLessThan("1.0", "1.1");
        }

        [Fact]
        public void Compare_prerelease_to_stable_version() {
            IsLessThan("1.0-FOO", "1.0");
        }

        [Fact]
        public void Compare_two_prerelease_versions_with_comparable_suffixes() {
            IsLessThan("1.0-RC1", "1.0-RC2");
        }

        [Fact]
        public void Compare_stable_to_prerelease_version() {
            IsLessThan("1.0", "1.1-WIP");
        }

        [Fact]
        public void Compare_two_prerelease_versions_with_incomparable_suffixes() {

            // arrange
            var version1 = VersionInfo.Parse("1.0-RC1");
            var version2 = VersionInfo.Parse("1.0-WIP");

            // act
            var result = version1.CompareToVersion(version2);

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public void Compare_version_to_null() {

            // arrange
            var version1 = VersionInfo.Parse("1.0-RC1");

            // act
            var result = version1.CompareToVersion(null);

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public void Compare_major_minor_version_to_major_minor_patch_version() {

            // arrange
            var version1 = VersionInfo.Parse("1.0");
            var version2 = VersionInfo.Parse("1.0.0");

            // act
            var result = version1.CompareToVersion(version2) == 0;

            // assert
            result.Should().Be(true);
        }

        private void IsLessThan(string left, string right) {

            // arrange
            var version1 = VersionInfo.Parse(left);
            var version2 = VersionInfo.Parse(right);

            // act
            var result = version1.CompareToVersion(version2) < 0;

            // assert
            result.Should().Be(true);
        }
    }
}
