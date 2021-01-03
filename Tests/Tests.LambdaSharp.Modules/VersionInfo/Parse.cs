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

    public class Parse {

        //--- Constructors ---
        public Parse(ITestOutputHelper output) => Output = output;

        //--- Properties ---
        private ITestOutputHelper Output { get; }

        //--- Methods ---

        [Fact]
        public void Zero_major() {

            // arrange
            var text = "0";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeFalse();
            version.IntegralMajor.Should().Be(0);
            version.Minor.Should().BeNull();
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void Zero_major_minor() {

            // arrange
            var text = "0.0";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeFalse();
            version.IntegralMajor.Should().Be(0);
            version.Minor.Should().Be(0);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void IntegralMajor() {

            // arrange
            var text = "7";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeFalse();
            version.IntegralMajor.Should().Be(7);
            version.Minor.Should().BeNull();
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void IntegralMajor_minor() {

            // arrange
            var text = "7.2";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeFalse();
            version.IntegralMajor.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void IntegralMajor_minor_suffix() {

            // arrange
            var text = "7.2-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeFalse();
            version.IntegralMajor.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("-DEV1");
        }

        [Fact]
        public void IntegralMajor_minor_patch() {

            // arrange
            var text = "7.2.3";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeFalse();
            version.IntegralMajor.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void IntegralMajor_minor_patch_suffix() {

            // arrange
            var text = "7.2.3-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeFalse();
            version.IntegralMajor.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Suffix.Should().Be("-DEV1");
        }

        [Fact]
        public void FractionalMajor() {

            // arrange
            var text = "0.7";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeTrue();
            version.FractionalMajor.Should().Be(7);
            version.Minor.Should().BeNull();
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void FractionalMajor_minor() {

            // arrange
            var text = "0.7.2";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeTrue();
            version.FractionalMajor.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void FractionalMajor_minor_suffix() {

            // arrange
            var text = "0.7.2-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeTrue();
            version.FractionalMajor.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().BeNull();
            version.Suffix.Should().Be("-DEV1");
        }

        [Fact]
        public void FractionalMajor_minor_patch() {

            // arrange
            var text = "0.7.2.3";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeTrue();
            version.FractionalMajor.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Suffix.Should().Be("");
        }

        [Fact]
        public void FractionalMajor_minor_patch_suffix() {

            // arrange
            var text = "0.7.2.3-DEV1";

            // act
            var version = VersionInfo.Parse(text);

            // assert
            version.IsFractionalMajor.Should().BeTrue();
            version.FractionalMajor.Should().Be(7);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Suffix.Should().Be("-DEV1");
        }
    }
}
