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

namespace LambdaSharp.Tool.Parser {

    public abstract class ASyntaxVisitor : ISyntaxVisitor {

        //--- Methods ---
        public virtual void VisitStart(ASyntaxNode parent, ModuleDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ModuleDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, UsingDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, UsingDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, ApiEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ApiEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, SchedulEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, SchedulEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, S3EventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, S3EventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, SlackCommandEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, SlackCommandEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, TopicEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, TopicEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, SqsEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, SqsEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, AlexaEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, AlexaEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, DynamoDBEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, DynamoDBEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, KinesisEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, KinesisEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, WebSocketEventSourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, WebSocketEventSourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, Base64FunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, Base64FunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, CidrFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, CidrFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, FindInMapExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, FindInMapExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, GetAttFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, GetAZsFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, GetAZsFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, IfFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, IfFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, ImportValueFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ImportValueFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, JoinFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, JoinFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, SelectFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, SelectFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, SplitFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, SplitFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, SubFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, SubFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, TransformFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, TransformFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ReferenceFunctionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, ParameterDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ParameterDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, ImportDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ImportDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, VariableDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, VariableDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, GroupDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, GroupDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, ConditionDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ConditionDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, ResourceDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ResourceDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, NestedModuleDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, PackageDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, PackageDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, FunctionDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, FunctionDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, FunctionDeclaration.VpcExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, FunctionDeclaration.VpcExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, MappingDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, MappingDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, MacroDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, MacroDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, TagListDeclaration node) { }
        public virtual void VisitEnd(ASyntaxNode parent, TagListDeclaration node) { }
        public virtual void VisitStart(ASyntaxNode parent, ObjectExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ObjectExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, ObjectExpression.KeyValuePair node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ObjectExpression.KeyValuePair node) { }
        public virtual void VisitStart(ASyntaxNode parent, ListExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ListExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, LiteralExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, LiteralExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, ConditionLiteralExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ConditionLiteralExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, ConditionNameExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ConditionNameExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, ConditionReferenceExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, ConditionReferenceExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, EqualsConditionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, EqualsConditionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, NotConditionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, NotConditionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, AndConditionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, AndConditionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, OrConditionExpression node) { }
        public virtual void VisitEnd(ASyntaxNode parent, OrConditionExpression node) { }
        public virtual void VisitStart(ASyntaxNode parent, MappingNameLiteral node) { }
        public virtual void VisitEnd(ASyntaxNode parent, MappingNameLiteral node) { }
    }
}