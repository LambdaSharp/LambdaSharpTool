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

    public class ParseVersion {

        //--- Fields ---
        private readonly ITestOutputHelper _output;

        //--- Constructors ---
        public ParseVersion(ITestOutputHelper output) => _output = output;

        //--- Methods ---

        [Fact]
        public void Major_minor() {

            // arrange
            var text = "7.2";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(7);
            version.MajorPartial.Should().BeNull();
            version.Minor.Should().Be(2);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void Major_minor_suffix() {

            // arrange
            var text = "7.2-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(7);
            version.MajorPartial.Should().BeNull();
            version.Minor.Should().Be(2);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("-DEV1");
        }

        [Fact]
        public void Major_minor_patch() {

            // arrange
            var text = "7.2.3";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(7);
            version.MajorPartial.Should().BeNull();
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void Major_minor_patch_suffix() {

            // arrange
            var text = "7.2.3-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(7);
            version.MajorPartial.Should().BeNull();
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Suffix.Should().Be("-DEV1");
        }

        [Fact]
        public void Fractional_major_minor() {

            // arrange
            var text = "0.7.2";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(0);
            version.MajorPartial.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void Fractional_major_minor_suffix() {

            // arrange
            var text = "0.7.2-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(0);
            version.MajorPartial.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("-DEV1");
        }

        [Fact]
        public void Fractional_major_minor_patch() {

            // arrange
            var text = "0.7.2.3";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(0);
            version.MajorPartial.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void Fractional_major_minor_patch_suffix() {

            // arrange
            var text = "0.7.2.3-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.Major.Should().Be(0);
            version.MajorPartial.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Suffix.Should().Be("-DEV1");
        }
    }
}
