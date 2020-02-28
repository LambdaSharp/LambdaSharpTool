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
        bool VisitStart(ModuleDeclaration node);
        ASyntaxNode? VisitEnd(ModuleDeclaration node);
        bool VisitStart(ModuleDeclaration.CloudFormationSpecExpression node);
        ASyntaxNode? VisitEnd(ModuleDeclaration.CloudFormationSpecExpression node);
        bool VisitStart(UsingModuleDeclaration node);
        ASyntaxNode? VisitEnd(UsingModuleDeclaration node);
        bool VisitStart(ApiEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(ApiEventSourceDeclaration node);
        bool VisitStart(SchedulEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(SchedulEventSourceDeclaration node);
        bool VisitStart(S3EventSourceDeclaration node);
        ASyntaxNode? VisitEnd(S3EventSourceDeclaration node);
        bool VisitStart(SlackCommandEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(SlackCommandEventSourceDeclaration node);
        bool VisitStart(TopicEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(TopicEventSourceDeclaration node);
        bool VisitStart(SqsEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(SqsEventSourceDeclaration node);
        bool VisitStart(AlexaEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(AlexaEventSourceDeclaration node);
        bool VisitStart(DynamoDBEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(DynamoDBEventSourceDeclaration node);
        bool VisitStart(KinesisEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(KinesisEventSourceDeclaration node);
        bool VisitStart(WebSocketEventSourceDeclaration node);
        ASyntaxNode? VisitEnd(WebSocketEventSourceDeclaration node);
        bool VisitStart(Base64FunctionExpression node);
        ASyntaxNode? VisitEnd(Base64FunctionExpression node);
        bool VisitStart(CidrFunctionExpression node);
        ASyntaxNode? VisitEnd(CidrFunctionExpression node);
        bool VisitStart(FindInMapFunctionExpression node);
        ASyntaxNode? VisitEnd(FindInMapFunctionExpression node);
        bool VisitStart(GetAttFunctionExpression node);
        ASyntaxNode? VisitEnd(GetAttFunctionExpression node);
        bool VisitStart(GetAZsFunctionExpression node);
        ASyntaxNode? VisitEnd(GetAZsFunctionExpression node);
        bool VisitStart(IfFunctionExpression node);
        ASyntaxNode? VisitEnd(IfFunctionExpression node);
        bool VisitStart(ImportValueFunctionExpression node);
        ASyntaxNode? VisitEnd(ImportValueFunctionExpression node);
        bool VisitStart(JoinFunctionExpression node);
        ASyntaxNode? VisitEnd(JoinFunctionExpression node);
        bool VisitStart(SelectFunctionExpression node);
        ASyntaxNode? VisitEnd(SelectFunctionExpression node);
        bool VisitStart(SplitFunctionExpression node);
        ASyntaxNode? VisitEnd(SplitFunctionExpression node);
        bool VisitStart(SubFunctionExpression node);
        ASyntaxNode? VisitEnd(SubFunctionExpression node);
        bool VisitStart(TransformFunctionExpression node);
        ASyntaxNode? VisitEnd(TransformFunctionExpression node);
        bool VisitStart(ReferenceFunctionExpression node);
        ASyntaxNode? VisitEnd(ReferenceFunctionExpression node);
        bool VisitStart(ParameterDeclaration node);
        ASyntaxNode? VisitEnd(ParameterDeclaration node);
        bool VisitStart(PseudoParameterDeclaration node);
        ASyntaxNode? VisitEnd(PseudoParameterDeclaration node);
        bool VisitStart(ImportDeclaration node);
        ASyntaxNode? VisitEnd(ImportDeclaration node);
        bool VisitStart(VariableDeclaration node);
        ASyntaxNode? VisitEnd(VariableDeclaration node);
        bool VisitStart(GroupDeclaration node);
        ASyntaxNode? VisitEnd(GroupDeclaration node);
        bool VisitStart(ConditionDeclaration node);
        ASyntaxNode? VisitEnd(ConditionDeclaration node);
        bool VisitStart(ResourceDeclaration node);
        ASyntaxNode? VisitEnd(ResourceDeclaration node);
        bool VisitStart(NestedModuleDeclaration node);
        ASyntaxNode? VisitEnd(NestedModuleDeclaration node);
        bool VisitStart(PackageDeclaration node);
        ASyntaxNode? VisitEnd(PackageDeclaration node);
        bool VisitStart(FunctionDeclaration node);
        ASyntaxNode? VisitEnd(FunctionDeclaration node);
        bool VisitStart(FunctionDeclaration.VpcExpression node);
        ASyntaxNode? VisitEnd(FunctionDeclaration.VpcExpression node);
        bool VisitStart(MappingDeclaration node);
        ASyntaxNode? VisitEnd(MappingDeclaration node);
        bool VisitStart(ResourceTypeDeclaration node);
        ASyntaxNode? VisitEnd(ResourceTypeDeclaration node);
        bool VisitStart(ResourceTypeDeclaration.PropertyTypeExpression node);
        ASyntaxNode? VisitEnd(ResourceTypeDeclaration.PropertyTypeExpression node);
        bool VisitStart(ResourceTypeDeclaration.AttributeTypeExpression node);
        ASyntaxNode? VisitEnd(ResourceTypeDeclaration.AttributeTypeExpression node);
        bool VisitStart(MacroDeclaration node);
        ASyntaxNode? VisitEnd(MacroDeclaration node);
        bool VisitStart(ObjectExpression node);
        ASyntaxNode? VisitEnd(ObjectExpression node);
        bool VisitStart(ListExpression node);
        ASyntaxNode? VisitEnd(ListExpression node);
        bool VisitStart(LiteralExpression node);
        ASyntaxNode? VisitEnd(LiteralExpression node);
        bool VisitStart(ConditionExpression node);
        ASyntaxNode? VisitEnd(ConditionExpression node);
        bool VisitStart(EqualsConditionExpression node);
        ASyntaxNode? VisitEnd(EqualsConditionExpression node);
        bool VisitStart(NotConditionExpression node);
        ASyntaxNode? VisitEnd(NotConditionExpression node);
        bool VisitStart(AndConditionExpression node);
        ASyntaxNode? VisitEnd(AndConditionExpression node);
        bool VisitStart(OrConditionExpression node);
        ASyntaxNode? VisitEnd(OrConditionExpression node);
    }
}