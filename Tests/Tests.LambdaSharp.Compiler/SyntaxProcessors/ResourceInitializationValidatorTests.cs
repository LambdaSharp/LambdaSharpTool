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
using LambdaSharp.Compiler.TypeSystem;
using LambdaSharp.Compiler.SyntaxProcessors;
using Tests.LambdaSharp.Compiler.TypeSystem.CloudFormation;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.SyntaxProcessors {

    public class ResourceInitializationValidatorTests :
        _SyntaxProcessor,
        ISyntaxProcessorDependencyProvider,
        IClassFixture<CloudFormationTypeSystemFixture>
    {

        // TODO: add tests using Fn::Transform when configuring a resource

        //--- Fields ---
        private readonly CloudFormationTypeSystemFixture _typeSystemFixture;

        //--- Constructors ---
        public ResourceInitializationValidatorTests(ITestOutputHelper output, CloudFormationTypeSystemFixture typeSystemFixture) : base(output)
            => _typeSystemFixture = typeSystemFixture ?? throw new System.ArgumentNullException(nameof(typeSystemFixture));

        //--- Methods ---

        [Fact]
        public void ValidateResourceTypeName() {

            // arrange
            var module = LoadTestModule();

            // act
            new ResourceInitializationValidator(this).Validate(module);

            // assert
            ExpectedMessages();
        }

        [Fact]
        public void ValidateUnknownResourceProperty() {

            // arrange
            var module = LoadTestModule();

            // act
            new ResourceInitializationValidator(this).Validate(module);

            // assert
            ExpectedMessages("ERROR: unrecognized property 'Acme' in Module.yml: line 7, column 7");
        }

        [Fact]
        public void ValidateMissingResourceProperty() {

            // arrange
            var module = LoadTestModule();

            // act
            new ResourceInitializationValidator(this).Validate(module);

            // assert
            ExpectedMessages("ERROR: missing property 'Endpoint' in Module.yml: line 9, column 11");
        }

        [Fact]
        public void ValidateOptionalResourceProperty() {

            // arrange
            var module = LoadTestModule();

            // act
            new ResourceInitializationValidator(this).Validate(module);

            // assert
            ExpectedMessages();
        }


        //--- Properties ---
        protected ITypeSystem TypeSystem => _typeSystemFixture.TypeSystem;

        //--- IModuleProcessorDependencyProvider Members ---
        bool ISyntaxProcessorDependencyProvider.TryGetResourceType(string typeName, [NotNullWhen(true)] out IResourceType? resourceType)
            => TypeSystem.TryGetResourceType(typeName, out resourceType);
    }
}