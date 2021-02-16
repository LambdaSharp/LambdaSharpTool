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

    public class MatchesConstraint {

        //--- Constructors ---
        public MatchesConstraint(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Suffix_must_match() {

            // arrange
            var version = VersionInfo.Parse("0.7-RC1");
            var versionConstraint = VersionInfo.Parse("0.7-WIP");

            // act
            var result = version.MatchesConstraint(versionConstraint);

            // assert
            result.Should().Be(false);
        }

        [Fact]
        public void Patch_constraint_must_be_met() {

            // arrange
            var version = VersionInfo.Parse("0.6");
            var versionConstraint = VersionInfo.Parse("0.6.0.2");

            // act
            var result = version.MatchesConstraint(versionConstraint);

            // assert
            result.Should().Be(false);
        }

        [Fact]
        public void Patch_version_is_ignored() {

            // arrange
            var version = VersionInfo.Parse("0.6.1.2");
            var versionConstraint = VersionInfo.Parse("0.6.1");

            // act
            var result = version.MatchesConstraint(versionConstraint);

            // assert
            result.Should().Be(true);
        }

        [Fact]
        public void FractionalMajor_Minor_version_is_ignored() {

            // arrange
            var version = VersionInfo.Parse("0.6.1.2");
            var versionConstraint = VersionInfo.Parse("0.6");

            // act
            var result = version.MatchesConstraint(versionConstraint);

            // assert
            result.Should().Be(true);
        }

        [Fact]
        public void IntegralMajor_Minor_version_is_ignored() {

            // arrange
            var version = VersionInfo.Parse("6.1.2");
            var versionConstraint = VersionInfo.Parse("6");

            // act
            var result = version.MatchesConstraint(versionConstraint);

            // assert
            result.Should().Be(true);
        }
    }
}
