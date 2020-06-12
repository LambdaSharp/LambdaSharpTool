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

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using LambdaSharp.Compiler.TypeSystem;
using LambdaSharp.Compiler.Processors;
using Tests.LambdaSharp.Compiler.TypeSystem.CloudFormation;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.Processors {

    public class ResourceDeclarationProcessorTests :
        _Processor,
        IProcessorDependencyProvider,
        IClassFixture<CloudFormationTypeSystemFixture>
    {

        //--- Fields ---
        private readonly CloudFormationTypeSystemFixture _typeSystemFixture;

        //--- Constructors ---
        public ResourceDeclarationProcessorTests(ITestOutputHelper output, CloudFormationTypeSystemFixture typeSystemFixture) : base(output)
            => _typeSystemFixture = typeSystemFixture ?? throw new System.ArgumentNullException(nameof(typeSystemFixture));

        //--- Methods ---

        [Fact]
        public void ValidateResourceTypeName() {

            // arrange
            var module = LoadTestModule();

            // act
            new ResourceDeclarationProcessor(this).Validate(module);

            // assert
            ExpectedMessages();
        }

        [Fact]
        public void ValidateUnknownResourceProperty() {

            // arrange
            var module = LoadTestModule();

            // act
            new ResourceDeclarationProcessor(this).Validate(module);

            // assert
            ExpectedMessages("ERROR: unrecognized property 'Acme' in Module.yml: line 7, column 7");
        }

        [Fact]
        public void ValidateMissingResourceProperty() {

            // arrange
            var module = LoadTestModule();

            // act
            new ResourceDeclarationProcessor(this).Validate(module);

            // assert
            ExpectedMessages("ERROR: missing property 'Endpoint' in Module.yml: line 9, column 11");
        }

        [Fact]
        public void ValidateOptionalResourceProperty() {

            // arrange
            var module = LoadTestModule();

            // act
            new ResourceDeclarationProcessor(this).Validate(module);

            // assert
            ExpectedMessages();
        }


        //--- Properties ---
        protected ITypeSystem TypeSystem => _typeSystemFixture.TypeSystem;

        //--- IModuleProcessorDependencyProvider Members ---
        bool IProcessorDependencyProvider.TryGetResourceType(string typeName, [NotNullWhen(true)] out IResourceType? resourceType)
            => TypeSystem.TryGetResourceType(typeName, out resourceType);
    }
}