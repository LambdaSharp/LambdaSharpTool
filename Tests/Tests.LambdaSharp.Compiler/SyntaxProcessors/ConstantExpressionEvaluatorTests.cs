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
using LambdaSharp.Compiler.SyntaxProcessors;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.SyntaxProcessors {

    public class ConstantExpressionEvaluatorTests : _SyntaxProcessor {

        //--- Constructors ---
        public ConstantExpressionEvaluatorTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void EvaluateIfTrueExpression() {

            // arrange
            var module = LoadTestModule();

            // act
            new ConstantExpressionProcessor(this).Process(module);

            // assert
            ExpectedMessages("WARNING: !If expression is always True in Module.yml: line 5, column 12");
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("It's true!");
        }

        [Fact]
        public void EvaluateIfFalseExpression() {

            // arrange
            var module = LoadTestModule();

            // act
            new ConstantExpressionProcessor(this).Process(module);

            // assert
            ExpectedMessages("WARNING: !If expression is always False in Module.yml: line 5, column 12");
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("It's false!");
        }

        [Fact]
        public void EvaluateIsDefinedTrueExpression() {

            // arrange
            var module = LoadTestModule();

            // act
            new ConstantExpressionProcessor(this).Process(module);

            // assert
            ExpectedMessages();
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("It's true!");
        }

        [Fact]
        public void EvaluateIsDefinedFalseExpression() {

            // arrange
            var module = LoadTestModule();

            // act
            new ConstantExpressionProcessor(this).Process(module);

            // assert
            ExpectedMessages();
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("It's false!");
        }

        [Fact]
        public void EvaluateJoinExpression() {

            // arrange
            var module = LoadTestModule();

            // act
            new ConstantExpressionProcessor(this).Process(module);

            // assert
            ExpectedMessages();
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("Hello world !");
        }

        [Fact]
        public void EvaluateSubExpression() {

            // arrange
            var module = LoadTestModule();

            // act
            new ConstantExpressionProcessor(this).Process(module);

            // assert
            ExpectedMessages();
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("Hello world!");
        }
    }
}