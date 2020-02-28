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

namespace LambdaSharp.Tool.Compiler {

    public interface ISyntaxVisitor {

        //--- Methods ---
        void VisitStart(ASyntaxNode? parent, ModuleDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ModuleDeclaration node);
        void VisitStart(ASyntaxNode? parent, ModuleDeclaration.CloudFormationSpecExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ModuleDeclaration.CloudFormationSpecExpression node);
        void VisitStart(ASyntaxNode? parent, UsingModuleDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, UsingModuleDeclaration node);
        void VisitStart(ASyntaxNode? parent, ApiEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ApiEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, SchedulEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, SchedulEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, S3EventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, S3EventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, SlackCommandEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, SlackCommandEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, TopicEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, TopicEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, SqsEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, SqsEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, AlexaEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, AlexaEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, DynamoDBEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, DynamoDBEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, KinesisEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, KinesisEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, WebSocketEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, WebSocketEventSourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, Base64FunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, Base64FunctionExpression node);
        void VisitStart(ASyntaxNode? parent, CidrFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, CidrFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, FindInMapFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, FindInMapFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, GetAttFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, GetAttFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, GetAZsFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, GetAZsFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, IfFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, IfFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, ImportValueFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ImportValueFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, JoinFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, JoinFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, SelectFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, SelectFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, SplitFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, SplitFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, SubFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, SubFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, TransformFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, TransformFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, ReferenceFunctionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ReferenceFunctionExpression node);
        void VisitStart(ASyntaxNode? parent, ParameterDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ParameterDeclaration node);
        void VisitStart(ASyntaxNode? parent, PseudoParameterDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, PseudoParameterDeclaration node);
        void VisitStart(ASyntaxNode? parent, ImportDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ImportDeclaration node);
        void VisitStart(ASyntaxNode? parent, VariableDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, VariableDeclaration node);
        void VisitStart(ASyntaxNode? parent, GroupDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, GroupDeclaration node);
        void VisitStart(ASyntaxNode? parent, ConditionDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ConditionDeclaration node);
        void VisitStart(ASyntaxNode? parent, ResourceDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ResourceDeclaration node);
        void VisitStart(ASyntaxNode? parent, NestedModuleDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, NestedModuleDeclaration node);
        void VisitStart(ASyntaxNode? parent, PackageDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, PackageDeclaration node);
        void VisitStart(ASyntaxNode? parent, FunctionDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, FunctionDeclaration node);
        void VisitStart(ASyntaxNode? parent, FunctionDeclaration.VpcExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, FunctionDeclaration.VpcExpression node);
        void VisitStart(ASyntaxNode? parent, MappingDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, MappingDeclaration node);
        void VisitStart(ASyntaxNode? parent, ResourceTypeDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ResourceTypeDeclaration node);
        void VisitStart(ASyntaxNode? parent, ResourceTypeDeclaration.PropertyTypeExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ResourceTypeDeclaration.PropertyTypeExpression node);
        void VisitStart(ASyntaxNode? parent, ResourceTypeDeclaration.AttributeTypeExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ResourceTypeDeclaration.AttributeTypeExpression node);
        void VisitStart(ASyntaxNode? parent, MacroDeclaration node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, MacroDeclaration node);
        void VisitStart(ASyntaxNode? parent, ObjectExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ObjectExpression node);
        void VisitStart(ASyntaxNode? parent, ListExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ListExpression node);
        void VisitStart(ASyntaxNode? parent, LiteralExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, LiteralExpression node);
        void VisitStart(ASyntaxNode? parent, ConditionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, ConditionExpression node);
        void VisitStart(ASyntaxNode? parent, EqualsConditionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, EqualsConditionExpression node);
        void VisitStart(ASyntaxNode? parent, NotConditionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, NotConditionExpression node);
        void VisitStart(ASyntaxNode? parent, AndConditionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, AndConditionExpression node);
        void VisitStart(ASyntaxNode? parent, OrConditionExpression node);
        ASyntaxNode? VisitEnd(ASyntaxNode? parent, OrConditionExpression node);
    }
}