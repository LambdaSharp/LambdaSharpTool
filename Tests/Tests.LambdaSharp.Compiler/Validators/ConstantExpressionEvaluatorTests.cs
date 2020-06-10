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

    public class ConstantExpressionEvaluatorTests : _Init {

        //--- Constructors ---
        public ConstantExpressionEvaluatorTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void EvaluateIfCondition() {

            // arrange
            var parser = NewParser("@Validators/ConstantExpressionEvaluatorTests/IfExpression.yml");
            var module = parser.ParseModule();
            new ItemDeclarationValidator(this).Validate(module);
            ExpectedMessages();
            module.Should().NotBeNull();

            // act
            new ConstantExpressionEvaluator(this).Evaluate(module);

            // assert
            ExpectedMessages("WARNING: !If expression is always True @ test.yml(7,12)");
            module.Items[1].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("It's true!");
        }
    }
}