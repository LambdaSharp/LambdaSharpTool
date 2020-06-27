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

using System.Runtime.CompilerServices;
using FluentAssertions;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Compiler.SyntaxProcessors;
using Xunit;
using Xunit.Abstractions;

namespace Tests.LambdaSharp.Compiler.SyntaxProcessors {

    public class ExpressionEvaluatorTests : _SyntaxProcessor {

        //--- Constructors ---
        public ExpressionEvaluatorTests(ITestOutputHelper output) : base(output) { }

        //--- Methods ---

        [Fact]
        public void EvaluateIfTrueExpression() {

            // arrange
            var module = LoadTestModule();

            // act
            new ExpressionEvaluator(this).Evaluate();

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
            new ExpressionEvaluator(this).Evaluate();

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
            new ExpressionEvaluator(this).Evaluate();

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
            new ExpressionEvaluator(this).Evaluate();

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
            new ExpressionEvaluator(this).Evaluate();

            // assert
            ExpectedMessages();
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("Hello world !");
        }

        [Fact]
        public void EvaluateSubExpressionSafeLiterals() {

            // arrange
            var module = LoadTestModule();

            // act
            new ExpressionEvaluator(this).Evaluate();

            // assert
            ExpectedMessages();
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("Hello world!");
        }

        [Fact]
        public void EvaluateSubExpressionEscapedExpression() {

            // arrange
            var module = LoadTestModule();

            // act
            new ExpressionEvaluator(this).Evaluate();

            // assert
            ExpectedMessages();
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("${Text}");
        }

        [Fact]
        public void EvaluateSubExpressionUnsafeParameters() {

            // arrange
            var module = LoadTestModule();

            // act
            new ExpressionEvaluator(this).Evaluate();

            // assert
            ExpectedMessages();
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<LiteralExpression>()
                .Which.Value.Should().Be("${Hello} ${World}!");
        }

        [Fact]
        public void EvaluateSubExpressionMixed() {

            // arrange
            var module = LoadTestModule();

            // act
            new ExpressionEvaluator(this).Evaluate();

            // assert
            ExpectedMessages();
            var subFunction = module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<SubFunctionExpression>().Which;
            subFunction.FormatString.Value.Should().Be("Hello ${MyParameter}! ${!Keep}");
            subFunction.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void EvaluateSubExpressionWarnUnusedParameter() {

            // arrange
            var module = LoadTestModule();

            // act
            new ExpressionEvaluator(this).Evaluate();

            // assert
            ExpectedMessages(
                "WARNING: parameter 'VariableC' in never used in !Sub format string in Module.yml: line 9, column 9"
            );
            module.Items[0].Should().BeOfType<VariableDeclaration>()
                .Which.Value.Should().BeOfType<SubFunctionExpression>()
                .Which.FormatString.Value.Should().Be("Hello world!");
        }

        protected override ModuleDeclaration LoadTestModule([CallerMemberName] string testName = "") {
            var result = base.LoadTestModule(testName);
            new VariableDeclarationProcessor(this).Process(result);
            return result;
        }
    }
}