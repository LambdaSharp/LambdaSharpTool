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

using LambdaSharp.Tool.Parser.Syntax;

namespace LambdaSharp.Tool.Parser.Analyzers {

    public class SyntaxHierarchyAnalyzer : ISyntaxVisitor {

        //--- Methods ---
        public void VisitEnd(ASyntaxNode parent, ModuleDeclaration node) { }
        public void VisitEnd(ASyntaxNode parent, UsingDeclaration node) { }
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
        public void VisitEnd(ASyntaxNode parent, FindInMapExpression node) { }
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
        public void VisitEnd(ASyntaxNode parent, FunctionVpcDeclaration node) { }
        public void VisitEnd(ASyntaxNode parent, MappingDeclaration node) { }
        public void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration node) { }
        public void VisitEnd(ASyntaxNode parent, ResourcePropertyTypeDeclaration node) { }
        public void VisitEnd(ASyntaxNode parent, ResourceAttributeTypeDeclaration node) { }
        public void VisitEnd(ASyntaxNode parent, MacroDeclaration node) { }
        public void VisitEnd(ASyntaxNode parent, TagListDeclaration node) { }
        public void VisitEnd(ASyntaxNode parent, ObjectExpression node) { }
        public void VisitEnd(ASyntaxNode parent, ListExpression node) { }
        public void VisitEnd(ASyntaxNode parent, LiteralExpression node) { }
        public void VisitEnd(ASyntaxNode parent, EqualsConditionExpression node) { }
        public void VisitEnd(ASyntaxNode parent, NotConditionExpression node) { }
        public void VisitEnd(ASyntaxNode parent, AndConditionExpression node) { }
        public void VisitEnd(ASyntaxNode parent, OrConditionExpression node) { }
        public void VisitEnd(ASyntaxNode parent, ConditionNameConditionExpression node) { }
        public void VisitEnd(ASyntaxNode parent, MappingNameLiteral node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ModuleDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, UsingDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ApiEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, SchedulEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, S3EventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, SlackCommandEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, TopicEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, SqsEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, AlexaEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, DynamoDBEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, KinesisEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, WebSocketEventSourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, Base64FunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, CidrFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, FindInMapExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, GetAZsFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, IfFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ImportValueFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, JoinFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, SelectFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, SplitFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, SubFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, TransformFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ParameterDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ImportDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, VariableDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, GroupDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ConditionDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ResourceDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, PackageDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, FunctionDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, FunctionVpcDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, MappingDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ResourcePropertyTypeDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ResourceAttributeTypeDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, MacroDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, TagListDeclaration node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ObjectExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, ListExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, LiteralExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, EqualsConditionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, NotConditionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, AndConditionExpression node) => node.Parent = parent;
        public void VisitStart(ASyntaxNode parent, OrConditionExpression node) => node.Parent = parent;

        public void VisitStart(ASyntaxNode parent, ConditionNameConditionExpression node) {
            throw new System.NotImplementedException();
        }

        public void VisitStart(ASyntaxNode parent, MappingNameLiteral node) {
            throw new System.NotImplementedException();
        }
    }
}
