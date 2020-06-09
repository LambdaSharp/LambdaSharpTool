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
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.EventSources;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace Tests.LambdaSharp.Compiler.Parser {

    public class SyntaxHierarchyValidationAnalyzer : ISyntaxVisitor {

        //--- Methods ---
        public ASyntaxNode VisitEnd(ModuleDeclaration node) => node;

        public ASyntaxNode VisitEnd(ModuleDeclaration.CloudFormationSpecExpression node) => node;

        public ASyntaxNode VisitEnd(UsingModuleDeclaration node) => node;

        public ASyntaxNode VisitEnd(ApiEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(SchedulEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(S3EventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(SlackCommandEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(TopicEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(SqsEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(AlexaEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(DynamoDBEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(KinesisEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(WebSocketEventSourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(Base64FunctionExpression node) => node;

        public ASyntaxNode VisitEnd(CidrFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(FindInMapFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(GetAttFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(GetAZsFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(IfFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(ImportValueFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(JoinFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(SelectFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(SplitFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(SubFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(TransformFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(ReferenceFunctionExpression node) => node;

        public ASyntaxNode VisitEnd(ParameterDeclaration node) => node;

        public ASyntaxNode VisitEnd(PseudoParameterDeclaration node) => node;

        public ASyntaxNode VisitEnd(ImportDeclaration node) => node;

        public ASyntaxNode VisitEnd(VariableDeclaration node) => node;

        public ASyntaxNode VisitEnd(GroupDeclaration node) => node;

        public ASyntaxNode VisitEnd(ConditionDeclaration node) => node;

        public ASyntaxNode VisitEnd(ResourceDeclaration node) => node;

        public ASyntaxNode VisitEnd(NestedModuleDeclaration node) => node;

        public ASyntaxNode VisitEnd(PackageDeclaration node) => node;

        public ASyntaxNode VisitEnd(FunctionDeclaration node) => node;

        public ASyntaxNode VisitEnd(FunctionDeclaration.VpcExpression node) => node;

        public ASyntaxNode VisitEnd(MappingDeclaration node) => node;

        public ASyntaxNode VisitEnd(ResourceTypeDeclaration node) => node;

        public ASyntaxNode VisitEnd(ResourceTypeDeclaration.PropertyTypeExpression node) => node;

        public ASyntaxNode VisitEnd(ResourceTypeDeclaration.AttributeTypeExpression node) => node;

        public ASyntaxNode VisitEnd(MacroDeclaration node) => node;

        public ASyntaxNode VisitEnd(ObjectExpression node) => node;

        public ASyntaxNode VisitEnd(ListExpression node) => node;

        public ASyntaxNode VisitEnd(LiteralExpression node) => node;

        public ASyntaxNode VisitEnd(ConditionExpression node) => node;

        public ASyntaxNode VisitEnd(EqualsConditionExpression node) => node;

        public ASyntaxNode VisitEnd(NotConditionExpression node) => node;

        public ASyntaxNode VisitEnd(AndConditionExpression node) => node;

        public ASyntaxNode VisitEnd(OrConditionExpression node) => node;

        public bool VisitStart(ModuleDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ModuleDeclaration.CloudFormationSpecExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(UsingModuleDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ApiEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(SchedulEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(S3EventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(SlackCommandEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(TopicEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(SqsEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(AlexaEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(DynamoDBEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(KinesisEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(WebSocketEventSourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(Base64FunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(CidrFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(FindInMapFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(GetAttFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(GetAZsFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(IfFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ImportValueFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(JoinFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(SelectFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(SplitFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(SubFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(TransformFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ReferenceFunctionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ParameterDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(PseudoParameterDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ImportDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(VariableDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(GroupDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ConditionDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ResourceDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(NestedModuleDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(PackageDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(FunctionDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(FunctionDeclaration.VpcExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(MappingDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ResourceTypeDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ResourceTypeDeclaration.PropertyTypeExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ResourceTypeDeclaration.AttributeTypeExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(MacroDeclaration node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ObjectExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ListExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(LiteralExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(ConditionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(EqualsConditionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(NotConditionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(AndConditionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        public bool VisitStart(OrConditionExpression node) {
            ValidateSyntaxNode(node, validateParent: false);
            return true;
        }

        private void ValidateSyntaxNode(ASyntaxNode node, bool validateParent = true) {
            if(validateParent) {
                node.Parent.Should().NotBeNull();
            }
            node.SourceLocation.Should().NotBeNull();
        }
    }
}