/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
            version.Version.Revision.Should().Be(-1);
            version.Version.Build.Should().Be(-1);
            version.Suffix.Should().Be("-DEV1");
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

        private void IsLessThan(string left, string right) {

            // arrange
            var version1 = VersionInfo.Parse(left);
            var version2 = VersionInfo.Parse(right);

            // act
            var result = version1.CompareToVersion(version2);

            // assert
            result.Should().Be(-1);
        }
    }
}
