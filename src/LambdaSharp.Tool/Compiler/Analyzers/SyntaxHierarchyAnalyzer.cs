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

using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class SyntaxHierarchyAnalyzer : ISyntaxVisitor {

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public SyntaxHierarchyAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---

        #region === Module Declarations ===
        public void VisitStart(ASyntaxNode parent, ModuleDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ModuleDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, UsingDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, UsingDeclaration node) { }
        #endregion

        #region === Item Declarations ===
        public void VisitStart(ASyntaxNode parent, ParameterDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ParameterDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, ImportDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ImportDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, VariableDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, VariableDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, GroupDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, GroupDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, ConditionDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ConditionDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, ResourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ResourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, NestedModuleDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, PackageDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, PackageDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, FunctionDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, FunctionDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, FunctionDeclaration.VpcExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, FunctionDeclaration.VpcExpression node) { }
        public void VisitStart(ASyntaxNode parent, MappingDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, MappingDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node) { }
        public void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node) { }
        public void VisitStart(ASyntaxNode parent, MacroDeclaration node) {
            InitializeSyntaxNode(parent, node);
            InitializeItemDeclaration(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, MacroDeclaration node) { }
        #endregion

        #region === Event Source Declarations ===
        public void VisitStart(ASyntaxNode parent, ApiEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ApiEventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, SchedulEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, SchedulEventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, S3EventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, S3EventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, SlackCommandEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, SlackCommandEventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, TopicEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, TopicEventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, SqsEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, SqsEventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, AlexaEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, AlexaEventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, DynamoDBEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, DynamoDBEventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, KinesisEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, KinesisEventSourceDeclaration node) { }
        public void VisitStart(ASyntaxNode parent, WebSocketEventSourceDeclaration node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, WebSocketEventSourceDeclaration node) { }
        #endregion

        #region === Value Expressions ===
        public void VisitStart(ASyntaxNode parent, Base64FunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, Base64FunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, CidrFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, CidrFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, FindInMapFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, FindInMapFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, GetAttFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, GetAZsFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, GetAZsFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, IfFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, IfFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, ImportValueFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ImportValueFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, JoinFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, JoinFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, SelectFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, SelectFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, SplitFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, SplitFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, SubFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, SubFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, TransformFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, TransformFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ReferenceFunctionExpression node) { }
        public void VisitStart(ASyntaxNode parent, ObjectExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ObjectExpression node) { }
        public void VisitEnd(ASyntaxNode parent, ObjectExpression.KeyValuePair node) { }
        public void VisitStart(ASyntaxNode parent, ListExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ListExpression node) { }
        public void VisitStart(ASyntaxNode parent, LiteralExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, LiteralExpression node) { }
        #endregion

        #region === Condition Expressions ===
        public void VisitStart(ASyntaxNode parent, EqualsConditionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, EqualsConditionExpression node) { }
        public void VisitStart(ASyntaxNode parent, NotConditionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, NotConditionExpression node) { }
        public void VisitStart(ASyntaxNode parent, AndConditionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, AndConditionExpression node) { }
        public void VisitStart(ASyntaxNode parent, OrConditionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, OrConditionExpression node) { }
        public void VisitStart(ASyntaxNode parent, ConditionExpression node) {
            InitializeSyntaxNode(parent, node);
        }
        public void VisitEnd(ASyntaxNode parent, ConditionExpression node) { }
        #endregion

        private void InitializeSyntaxNode(ASyntaxNode parent, ASyntaxNode node) {
            node.Parent ??= parent;
            node.SourceLocation ??= parent.SourceLocation;
        }

        private void InitializeItemDeclaration(ASyntaxNode parent, AItemDeclaration node) {

            // assign full name
            if(parent is AItemDeclaration parentItemDeclaration) {
                node.FullName = $"{parentItemDeclaration.FullName}::{node.LocalName}";
            } else {
                node.FullName = node.LocalName;
            }

            // TODO: we shouldn't always assign this expresion, because it's not always the correct thing to do
            // assign default reference expression
            node.ReferenceExpression = ASyntaxAnalyzer.FnRef(node.FullName);

            // register item declaration
            node.LogicalId = _builder.AddItemDeclaration(parent, node);
        }
    }
}
