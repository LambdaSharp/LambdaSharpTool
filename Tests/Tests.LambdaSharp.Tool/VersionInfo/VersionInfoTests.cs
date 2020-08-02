/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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
using LambdaSharp.Tool;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Tool.VersionInfoTests {

    public class VersionInfoTests {

        //--- Fields ---
        private readonly ITestOutputHelper _output;

        //--- Constructors ---
        public VersionInfoTests(ITestOutputHelper output) => _output = output;

        //--- Methods ---

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
            result.Should().BeNull();
        }

        [Fact]
        public void CompareNullVersion() {

            // arrange
            var version1 = VersionInfo.Parse("1.0-RC1");

            // act
            var result = version1.CompareToVersion(null);

            // assert
            result.Should().BeNull();
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
        public void GetLambdaSharpAssemblyWildcardVersionForNetCore21() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.8.1.2");
            var framework = "netcoreapp2.1";

            // act
            var assemblyVersion = toolVersion.GetLambdaSharpAssemblyWildcardVersion(framework);

            // assert
            assemblyVersion.Should().Be("0.8.0.*");
        }

        [Fact]
        public void GetLambdaSharpAssemblyWildcardVersionForNetCore31() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.8.1.2");
            var framework = "netcoreapp3.1";

            // act
            var assemblyVersion = toolVersion.GetLambdaSharpAssemblyWildcardVersion(framework);

            // assert
            assemblyVersion.Should().Be("0.8.1.*");
        }

        [Fact]
        public void IsValidLambdaSharpAssemblyReferenceForToolVersionForNetCore21() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.8.1.2");
            var framework = "netcoreapp2.1";
            var lambdaSharpAssemblyVersion = "0.8.0.*";

            // act
            var result = VersionInfo.IsValidLambdaSharpAssemblyReferenceForToolVersion(toolVersion, framework, lambdaSharpAssemblyVersion);

            // assert
            result.Should().Be(true);
        }

        [Fact]
        public void IsValidLambdaSharpAssemblyReferenceForToolVersionForNetCore31() {

            // arrange
            var toolVersion = VersionInfo.Parse("0.8.1.2");
            var framework = "netcoreapp3.1";
            var lambdaSharpAssemblyVersion = "0.8.1.*";

            // act
            var result = VersionInfo.IsValidLambdaSharpAssemblyReferenceForToolVersion(toolVersion, framework, lambdaSharpAssemblyVersion);

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
