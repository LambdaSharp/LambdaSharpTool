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

    public interface ISyntaxVisitor {

        //--- Methods ---
        void VisitStart(ASyntaxNode parent, ModuleDeclaration node);
        void VisitEnd(ASyntaxNode parent, ModuleDeclaration node);
        void VisitStart(ASyntaxNode parent, UsingDeclaration node);
        void VisitEnd(ASyntaxNode parent, UsingDeclaration node);
        void VisitStart(ASyntaxNode parent, ApiEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, ApiEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, SchedulEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, SchedulEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, S3EventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, S3EventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, SlackCommandEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, SlackCommandEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, TopicEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, TopicEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, SqsEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, SqsEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, AlexaEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, AlexaEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, DynamoDBEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, DynamoDBEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, KinesisEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, KinesisEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, WebSocketEventSourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, WebSocketEventSourceDeclaration node);
        void VisitStart(ASyntaxNode parent, Base64FunctionExpression node);
        void VisitEnd(ASyntaxNode parent, Base64FunctionExpression node);
        void VisitStart(ASyntaxNode parent, CidrFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, CidrFunctionExpression node);
        void VisitStart(ASyntaxNode parent, FindInMapExpression node);
        void VisitEnd(ASyntaxNode parent, FindInMapExpression node);
        void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, GetAttFunctionExpression node);
        void VisitStart(ASyntaxNode parent, GetAZsFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, GetAZsFunctionExpression node);
        void VisitStart(ASyntaxNode parent, IfFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, IfFunctionExpression node);
        void VisitStart(ASyntaxNode parent, ImportValueFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, ImportValueFunctionExpression node);
        void VisitStart(ASyntaxNode parent, JoinFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, JoinFunctionExpression node);
        void VisitStart(ASyntaxNode parent, SelectFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, SelectFunctionExpression node);
        void VisitStart(ASyntaxNode parent, SplitFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, SplitFunctionExpression node);
        void VisitStart(ASyntaxNode parent, SubFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, SubFunctionExpression node);
        void VisitStart(ASyntaxNode parent, TransformFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, TransformFunctionExpression node);
        void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node);
        void VisitEnd(ASyntaxNode parent, ReferenceFunctionExpression node);
        void VisitStart(ASyntaxNode parent, ParameterDeclaration node);
        void VisitEnd(ASyntaxNode parent, ParameterDeclaration node);
        void VisitStart(ASyntaxNode parent, ImportDeclaration node);
        void VisitEnd(ASyntaxNode parent, ImportDeclaration node);
        void VisitStart(ASyntaxNode parent, VariableDeclaration node);
        void VisitEnd(ASyntaxNode parent, VariableDeclaration node);
        void VisitStart(ASyntaxNode parent, GroupDeclaration node);
        void VisitEnd(ASyntaxNode parent, GroupDeclaration node);
        void VisitStart(ASyntaxNode parent, ConditionDeclaration node);
        void VisitEnd(ASyntaxNode parent, ConditionDeclaration node);
        void VisitStart(ASyntaxNode parent, ResourceDeclaration node);
        void VisitEnd(ASyntaxNode parent, ResourceDeclaration node);
        void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node);
        void VisitEnd(ASyntaxNode parent, NestedModuleDeclaration node);
        void VisitStart(ASyntaxNode parent, PackageDeclaration node);
        void VisitEnd(ASyntaxNode parent, PackageDeclaration node);
        void VisitStart(ASyntaxNode parent, FunctionDeclaration node);
        void VisitEnd(ASyntaxNode parent, FunctionDeclaration node);
        void VisitStart(ASyntaxNode parent, FunctionDeclaration.VpcExpression node);
        void VisitEnd(ASyntaxNode parent, FunctionDeclaration.VpcExpression node);
        void VisitStart(ASyntaxNode parent, MappingDeclaration node);
        void VisitEnd(ASyntaxNode parent, MappingDeclaration node);
        void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node);
        void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration node);
        void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node);
        void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration.PropertyTypeExpression node);
        void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node);
        void VisitEnd(ASyntaxNode parent, ResourceTypeDeclaration.AttributeTypeExpression node);
        void VisitStart(ASyntaxNode parent, MacroDeclaration node);
        void VisitEnd(ASyntaxNode parent, MacroDeclaration node);
        void VisitStart(ASyntaxNode parent, TagListDeclaration node);
        void VisitEnd(ASyntaxNode parent, TagListDeclaration node);
        void VisitStart(ASyntaxNode parent, ObjectExpression node);
        void VisitEnd(ASyntaxNode parent, ObjectExpression node);
        void VisitStart(ASyntaxNode parent, ObjectExpression.KeyValuePair node);
        void VisitEnd(ASyntaxNode parent, ObjectExpression.KeyValuePair node);
        void VisitStart(ASyntaxNode parent, ListExpression node);
        void VisitEnd(ASyntaxNode parent, ListExpression node);
        void VisitStart(ASyntaxNode parent, LiteralExpression node);
        void VisitEnd(ASyntaxNode parent, LiteralExpression node);
        void VisitStart(ASyntaxNode parent, ConditionRefExpression node);
        void VisitEnd(ASyntaxNode parent, ConditionRefExpression node);
        void VisitStart(ASyntaxNode parent, EqualsConditionExpression node);
        void VisitEnd(ASyntaxNode parent, EqualsConditionExpression node);
        void VisitStart(ASyntaxNode parent, NotConditionExpression node);
        void VisitEnd(ASyntaxNode parent, NotConditionExpression node);
        void VisitStart(ASyntaxNode parent, AndConditionExpression node);
        void VisitEnd(ASyntaxNode parent, AndConditionExpression node);
        void VisitStart(ASyntaxNode parent, OrConditionExpression node);
        void VisitEnd(ASyntaxNode parent, OrConditionExpression node);
    }
}