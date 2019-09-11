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

using System.Collections.Generic;
using FluentAssertions;
using LambdaSharp.Tool;
using Xunit;

namespace Tests.LambdaSharp.Tool {

    public class VersionInfoTests {

        //--- Methods ---

        [Fact]
        public void ParseVersion() {

            // arrange
            var text = "7.2-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(null);
            version.PatchRevision.Should().Be(null);
            version.Suffix.Should().Be("-DEV1");
        }

        [Fact]
        public void ToStringMajorMinorVersion() {

            // arrange
            var text = "7.2";

            // act
            var result = VersionInfo.Parse(text).ToString();

            // assert
            result.Should().Be(text);
        }

        [Fact]
        public void ToStringMajorMinorSuffixVersion() {

            // arrange
            var text = "7.2-DEV1";

            // act
            var result = VersionInfo.Parse(text).ToString();

            // assert
            result.Should().Be(text);
        }

        [Fact]
        public void ToStringMajorMinorPatchSuffixVersion() {

            // arrange
            var text = "7.2.3-DEV1";

            // act
            var result = VersionInfo.Parse(text).ToString();

            // assert
            result.Should().Be(text);
        }

        [Fact]
        public void ToStringMajorMinorPatchMinorSuffixVersion() {

            // arrange
            var text = "7.2.0.4-DEV1";

            // act
            var result = VersionInfo.Parse(text).ToString();

            // assert
            result.Should().Be(text);
        }

        [Fact]
        public void CompareTwoStableVersions() {
            IsLessThan("1.0", "1.1");
        }

        [Fact]
        public void ComparePreReleaseVersionToStableVersion() {
            IsLessThan("1.0-FOO", "1.0");
        }

        [Fact]
        public void CompareTwoPreReleaseVersions() {
            IsLessThan("1.0-RC1", "1.0-RC2");
        }

        [Fact]
        public void CompareStableVersionToPreReleaseVersion() {
            IsLessThan("1.0", "1.1-WIP");
        }

        [Fact]
        public void CompareIncomparableVersion() {

            // arrange
            var version1 = VersionInfo.Parse("1.0-RC1");
            var version2 = VersionInfo.Parse("1.0-WIP");

            // act
            var result = version1.CompareToVersion(version2);

            // assert
            result.Should().Be(null);
        }

        [Fact]
        public void CompareNullVersion() {

            // arrange
            var version1 = VersionInfo.Parse("1.0-RC1");

            // act
            var result = version1.CompareToVersion(null);

            // assert
            result.Should().Be(null);
        }

        [Fact]
        public void CompareMajorMinorToMajorMinorPatch() {

            // arrange
            var version1 = VersionInfo.Parse("1.0");
            var version2 = VersionInfo.Parse("1.0.0");

            // act
            var result = version1.CompareToVersion(version2) == 0;

            // assert
            result.Should().Be(true);
        }

        [Fact]
        public void CompareMajorMinorToMajorMinorPatchPatchMinor() {

            // arrange
            var version1 = VersionInfo.Parse("1.0");
            var version2 = VersionInfo.Parse("1.0.0.0");

            // act
            var result = version1.CompareToVersion(version2) == 0;

            // assert
            result.Should().Be(true);
        }

        [Fact]
        public void CompareMajorMinorPatchToMajorMinorPatchPatchMinor() {

            // arrange
            var version1 = VersionInfo.Parse("1.0.0");
            var version2 = VersionInfo.Parse("1.0.0.0");

            // act
            var result = version1.CompareToVersion(version2) == 0;

            // assert
            result.Should().Be(true);
        }

        [Fact]
        public void MismatchedSuffixDoesNotMatchTightConstraint() {

            // arrange
            var versionConstraint = VersionInfo.Parse("0.7-WIP");
            var version = VersionInfo.Parse("0.7-RC1");

            // act
            var result = version.MatchesConstraint(versionConstraint);

            // assert
            result.Should().Be(false);
        }

        [Fact]
        public void PreviousVersionDoesNotMatchTightConstraint() {

            // arrange
            var versionConstraint = VersionInfo.Parse("0.6.0.2");
            var version = VersionInfo.Parse("0.6");

            // act
            var result = version.MatchesConstraint(versionConstraint);

            // assert
            result.Should().Be(false);
        }

        [Fact]
        public void RevisionVersionMatchesTightConstraint() {

            // arrange
            var versionConstraint = VersionInfo.Parse("0.6.1");
            var version = VersionInfo.Parse("0.6.1.2");

            // act
            var result = version.MatchesConstraint(versionConstraint);

            // assert
            result.Should().Be(true);
        }

        [Fact]
        public void FindLatestMatchingVersionWithPreReleases() {

            // arrange
            var versions = new List<VersionInfo> {
                VersionInfo.Parse("0.7.0-wip"),
                VersionInfo.Parse("0.7.0-rc4"),
                VersionInfo.Parse("0.7.0-rc5"),
                VersionInfo.Parse("0.7.0-rc6"),
                VersionInfo.Parse("0.7.0"),
                VersionInfo.Parse("0.7.1"),
                VersionInfo.Parse("0.7.2"),
                VersionInfo.Parse("0.8.0")
            };
            var toolVersion = VersionInfo.Parse("0.7.0-rc6");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                versions,
                minVersion: null,
                coreVersion => coreVersion.IsCoreServicesCompatible(toolVersion)
            ).CompareToVersion(VersionInfo.Parse("0.7.0-rc6"));

            // assert
            result.Should().Be(0);
        }

        [Fact]
        public void FindLatestMatchingVersionWithPreReleasesAndMinVersion() {

            // arrange
            var versions = new List<VersionInfo> {
                VersionInfo.Parse("0.7.0-wip"),
                VersionInfo.Parse("0.7.0-rc4"),
                VersionInfo.Parse("0.7.0-rc5"),
                VersionInfo.Parse("0.7.0-rc6"),
                VersionInfo.Parse("0.7.0"),
                VersionInfo.Parse("0.7.1"),
                VersionInfo.Parse("0.7.2"),
                VersionInfo.Parse("0.8.0")
            };
            var toolVersion = VersionInfo.Parse("0.7.0-rc6");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                versions,
                minVersion: VersionInfo.Parse("0.7.0-rc5"),
                coreVersion => coreVersion.IsCoreServicesCompatible(toolVersion)
            ).CompareToVersion(VersionInfo.Parse("0.7.0-rc6"));

            // assert
            result.Should().Be(0);
        }

        [Fact]
        public void FindLatestMatchingVersion() {

            // arrange
            var versions = new List<VersionInfo> {
                VersionInfo.Parse("0.7.0-wip"),
                VersionInfo.Parse("0.7.0-rc4"),
                VersionInfo.Parse("0.7.0-rc5"),
                VersionInfo.Parse("0.7.0-rc6"),
                VersionInfo.Parse("0.7.0"),
                VersionInfo.Parse("0.7.1"),
                VersionInfo.Parse("0.7.2"),
                VersionInfo.Parse("0.8.0")
            };
            var toolVersion = VersionInfo.Parse("0.7.1");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                versions,
                minVersion: null,
                coreVersion => coreVersion.IsCoreServicesCompatible(toolVersion)
            ).CompareToVersion(VersionInfo.Parse("0.7.2"));

            // assert
            result.Should().Be(0);
        }

        [Fact]
        public void FindLatestMatchingVersionWithMinVersion() {

            // arrange
            var versions = new List<VersionInfo> {
                VersionInfo.Parse("0.7.0-wip"),
                VersionInfo.Parse("0.7.0-rc4"),
                VersionInfo.Parse("0.7.0-rc5"),
                VersionInfo.Parse("0.7.0-rc6"),
                VersionInfo.Parse("0.7.0"),
                VersionInfo.Parse("0.7.1"),
                VersionInfo.Parse("0.7.2"),
                VersionInfo.Parse("0.8.0")
            };
            var toolVersion = VersionInfo.Parse("0.7.1");

            // act
            var result = VersionInfo.FindLatestMatchingVersion(
                versions,
                minVersion: VersionInfo.Parse("0.7.0"),
                coreVersion => coreVersion.IsCoreServicesCompatible(toolVersion)
            ).CompareToVersion(VersionInfo.Parse("0.7.2"));

            // assert
            result.Should().Be(0);
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
