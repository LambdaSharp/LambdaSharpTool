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

using System;
using System.IO.Compression;
using System.Linq;
using FluentAssertions;
using LambdaSharp.Compiler.TypeSystem;
using LambdaSharp.Compiler.TypeSystem.CloudFormation;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.TypeSystem.CloudFormation {

    public class CloudFormationTypeSystemFixture : IDisposable {

        //--- Constructors ---
        public CloudFormationTypeSystemFixture() {
            using var stream = ResourceReader.OpenStream("us-east-1.json.br");
            using var compression = new BrotliStream(stream, CompressionMode.Decompress);
            TypeSystem = CloudFormationTypeSystem.LoadFromAsync(compression).Result;
        }

        //--- Properties ---
        public ITypeSystem TypeSystem { get; }

        //--- Methods ---
        public void Dispose() { }
    }

    public class CloudFormationTypeSystemTests : _Init, IClassFixture<CloudFormationTypeSystemFixture> {


        //--- Fields ---
        private readonly CloudFormationTypeSystemFixture _typeDirectoryFixture;

        //--- Constructors ---
        public CloudFormationTypeSystemTests(ITestOutputHelper output, CloudFormationTypeSystemFixture typeDirectoryFixture)
            : base(output)
        {
            _typeDirectoryFixture = typeDirectoryFixture ?? throw new ArgumentNullException(nameof(typeDirectoryFixture));
        }

        //--- Properties ---
        protected ITypeSystem TypeSystem => _typeDirectoryFixture.TypeSystem;

        //--- Methods ---

        [Fact]
        public void CountResourceTypes() {

            // act
            var count = TypeSystem.ResourceTypes.Count();

            // assert
            count.Should().Be(540);
        }
    }
}