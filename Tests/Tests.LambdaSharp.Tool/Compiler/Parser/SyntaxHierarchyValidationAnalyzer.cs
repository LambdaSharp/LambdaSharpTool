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

using FluentAssertions;
using LambdaSharp.Tool.Compiler;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace Tests.LambdaSharp.Tool.Compiler.Parser {

    public class SyntaxHierarchyValidationAnalyzer : ISyntaxVisitor {

        //--- Methods ---
        public void VisitEnd(ASyntaxNode parent, ModuleDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, UsingModuleDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, ApiEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, SchedulEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, S3EventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, SlackCommandEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, TopicEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, SqsEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, AlexaEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, DynamoDBEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, KinesisEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, WebSocketEventSourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, Base64FunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, CidrFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, FindInMapFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, GetAttFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, GetAZsFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, IfFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, ImportValueFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, JoinFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, SelectFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, SplitFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, SubFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, TransformFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, ReferenceFunctionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, ParameterDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, ImportDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, VariableDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, GroupDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, ConditionDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, ResourceDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, NestedModuleDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, PackageDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, FunctionDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, FunctionDeclaration.VpcExpression node) { }

        public void VisitEnd(ASyntaxNode parent, MappingDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node) { }

        public void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node) { }

        public void VisitEnd(ASyntaxNode parent, MacroDeclaration node) { }

        public void VisitEnd(ASyntaxNode parent, ObjectExpression node) { }

        public void VisitEnd(ASyntaxNode parent, ListExpression node) { }

        public void VisitEnd(ASyntaxNode parent, LiteralExpression node) { }

        public void VisitEnd(ASyntaxNode parent, ConditionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, EqualsConditionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, NotConditionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, AndConditionExpression node) { }

        public void VisitEnd(ASyntaxNode parent, OrConditionExpression node) { }

        public void VisitStart(ASyntaxNode parent, ModuleDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
        }

        public void VisitStart(ASyntaxNode parent, UsingModuleDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ApiEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, SchedulEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, S3EventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, SlackCommandEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, TopicEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, SqsEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, AlexaEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, DynamoDBEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, KinesisEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, WebSocketEventSourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, Base64FunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, CidrFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, FindInMapFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, GetAZsFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, IfFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ImportValueFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, JoinFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, SelectFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, SplitFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, SubFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, TransformFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ParameterDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ImportDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, VariableDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, GroupDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ConditionDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ResourceDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, PackageDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, FunctionDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, FunctionDeclaration.VpcExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, MappingDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, MacroDeclaration node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ObjectExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ListExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, LiteralExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, ConditionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, EqualsConditionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, NotConditionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, AndConditionExpression node) {
            ValidateSyntaxNode(node);
        }

        public void VisitStart(ASyntaxNode parent, OrConditionExpression node) {
            ValidateSyntaxNode(node);
        }

        private void ValidateSyntaxNode(ASyntaxNode node, bool validateParent = true) {
            if(validateParent) {
                node.Parent.Should().NotBeNull();
            }
            node.SourceLocation.Should().NotBeNull();
        }
    }
}