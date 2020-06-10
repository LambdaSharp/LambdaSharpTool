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

#nullable disable

using FluentAssertions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Compiler.Validators;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.Validators {

    // TODO: test that circular dependencies are also detected for 'DependsOn'

    public class ReferenceValidatorTests : _Init {

        //--- Constructors ---
        public ReferenceValidatorTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void MultiOverlappingCircularDependencies() {

            // arrange
            var parser = NewParser("@Validators/ReferenceValidatorTests/MultiOverlappingCircularDependencies.yml");
            var module = parser.ParseModule();
            new ItemDeclarationValidator(this).Validate(module);
            ExpectedMessages();
            module.Should().NotBeNull();

            // act
            new ReferenceValidator(this).Validate(module, Declarations);

            // assert
            ExpectedMessages(
                "ERROR: circular dependency VariableA -> VariableB -> VariableC -> VariableA in Module.yml: line 4, column 5",
                "ERROR: circular dependency VariableA -> VariableB -> VariableC -> VariableD -> VariableE -> VariableA in Module.yml: line 4, column 5"
            );
        }

        [Fact]
        public void MultiDistinctCircularDependencies() {

            // arrange
            var parser = NewParser("@Validators/ReferenceValidatorTests/MultiDistinctCircularDependencies.yml");
            var module = parser.ParseModule();
            new ItemDeclarationValidator(this).Validate(module);
            ExpectedMessages();
            module.Should().NotBeNull();

            // act
            new ReferenceValidator(this).Validate(module, Declarations);

            // assert
            ExpectedMessages(
                "ERROR: circular dependency VariableA -> VariableB -> VariableC -> VariableA in Module.yml: line 4, column 5",
                "ERROR: circular dependency VariableE -> VariableF -> VariableE in Module.yml: line 16, column 5"
            );
        }

        [Fact]
        public void SelfCircularDependency() {

            // arrange
            var parser = NewParser("@Validators/ReferenceValidatorTests/SelfCircularDependencies.yml");
            var module = parser.ParseModule();
            new ItemDeclarationValidator(this).Validate(module);
            ExpectedMessages();
            module.Should().NotBeNull();

            // act
            new ReferenceValidator(this).Validate(module, Declarations);

            // assert
            ExpectedMessages("ERROR: circular dependency VariableA -> VariableA in Module.yml: line 4, column 5");
        }

        [Fact]
        public void DependsOnCircularDependency() {

            // arrange
            var parser = NewParser("@Validators/ReferenceValidatorTests/DependsOnCircularDependencies.yml");
            var module = parser.ParseModule();
            new ItemDeclarationValidator(this).Validate(module);
            ExpectedMessages();
            module.Should().NotBeNull();

            // act
            new ReferenceValidator(this).Validate(module, Declarations);

            // assert
            ExpectedMessages("ERROR: circular dependency ResourceA -> ResourceB -> ResourceA in Module.yml: line 4, column 5");
        }
    }
}