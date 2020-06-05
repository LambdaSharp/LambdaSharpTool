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

namespace LambdaSharp.Compiler.Syntax {

    public abstract class ASyntaxAnalyzer : ISyntaxVisitor {

        //--- Class Methods ---


        //--- Methods ---
        public virtual bool VisitStart(ModuleDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(ModuleDeclaration node) => node;
        public bool VisitStart(ModuleDeclaration.CloudFormationSpecExpression node) => true;
        public ASyntaxNode? VisitEnd(ModuleDeclaration.CloudFormationSpecExpression node) => node;
        public virtual bool VisitStart(UsingModuleDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(UsingModuleDeclaration node) => node;
        public virtual bool VisitStart(ApiEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(ApiEventSourceDeclaration node) => node;
        public virtual bool VisitStart(SchedulEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(SchedulEventSourceDeclaration node) => node;
        public virtual bool VisitStart(S3EventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(S3EventSourceDeclaration node) => node;
        public virtual bool VisitStart(SlackCommandEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(SlackCommandEventSourceDeclaration node) => node;
        public virtual bool VisitStart(TopicEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(TopicEventSourceDeclaration node) => node;
        public virtual bool VisitStart(SqsEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(SqsEventSourceDeclaration node) => node;
        public virtual bool VisitStart(AlexaEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(AlexaEventSourceDeclaration node) => node;
        public virtual bool VisitStart(DynamoDBEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(DynamoDBEventSourceDeclaration node) => node;
        public virtual bool VisitStart(KinesisEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(KinesisEventSourceDeclaration node) => node;
        public virtual bool VisitStart(WebSocketEventSourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(WebSocketEventSourceDeclaration node) => node;
        public virtual bool VisitStart(Base64FunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(Base64FunctionExpression node) => node;
        public virtual bool VisitStart(CidrFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(CidrFunctionExpression node) => node;
        public virtual bool VisitStart(FindInMapFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(FindInMapFunctionExpression node) => node;
        public virtual bool VisitStart(GetAttFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(GetAttFunctionExpression node) => node;
        public virtual bool VisitStart(GetAZsFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(GetAZsFunctionExpression node) => node;
        public virtual bool VisitStart(IfFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(IfFunctionExpression node) => node;
        public virtual bool VisitStart(ImportValueFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(ImportValueFunctionExpression node) => node;
        public virtual bool VisitStart(JoinFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(JoinFunctionExpression node) => node;
        public virtual bool VisitStart(SelectFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(SelectFunctionExpression node) => node;
        public virtual bool VisitStart(SplitFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(SplitFunctionExpression node) => node;
        public virtual bool VisitStart(SubFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(SubFunctionExpression node) => node;
        public virtual bool VisitStart(TransformFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(TransformFunctionExpression node) => node;
        public virtual bool VisitStart(ReferenceFunctionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(ReferenceFunctionExpression node) => node;
        public virtual bool VisitStart(ParameterDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(ParameterDeclaration node) => node;
        public virtual bool VisitStart(PseudoParameterDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(PseudoParameterDeclaration node) => node;
        public virtual bool VisitStart(ImportDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(ImportDeclaration node) => node;
        public virtual bool VisitStart(VariableDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(VariableDeclaration node) => node;
        public virtual bool VisitStart(GroupDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(GroupDeclaration node) => node;
        public virtual bool VisitStart(ConditionDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(ConditionDeclaration node) => node;
        public virtual bool VisitStart(ResourceDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(ResourceDeclaration node) => node;
        public virtual bool VisitStart(NestedModuleDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(NestedModuleDeclaration node) => node;
        public virtual bool VisitStart(PackageDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(PackageDeclaration node) => node;
        public virtual bool VisitStart(FunctionDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(FunctionDeclaration node) => node;
        public virtual bool VisitStart(FunctionDeclaration.VpcExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(FunctionDeclaration.VpcExpression node) => node;
        public virtual bool VisitStart(MappingDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(MappingDeclaration node) => node;
        public virtual bool VisitStart(ResourceTypeDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(ResourceTypeDeclaration node) => node;
        public virtual bool VisitStart(ResourceTypeDeclaration.PropertyTypeExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(ResourceTypeDeclaration.PropertyTypeExpression node) => node;
        public virtual bool VisitStart(ResourceTypeDeclaration.AttributeTypeExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(ResourceTypeDeclaration.AttributeTypeExpression node) => node;
        public virtual bool VisitStart(MacroDeclaration node) => true;
        public virtual ASyntaxNode? VisitEnd(MacroDeclaration node) => node;
        public virtual bool VisitStart(ObjectExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(ObjectExpression node) => node;
        public virtual bool VisitStart(ListExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(ListExpression node) => node;
        public virtual bool VisitStart(LiteralExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(LiteralExpression node) => node;
        public virtual bool VisitStart(ConditionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(ConditionExpression node) => node;
        public virtual bool VisitStart(EqualsConditionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(EqualsConditionExpression node) => node;
        public virtual bool VisitStart(NotConditionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(NotConditionExpression node) => node;
        public virtual bool VisitStart(AndConditionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(AndConditionExpression node) => node;
        public virtual bool VisitStart(OrConditionExpression node) => true;
        public virtual ASyntaxNode? VisitEnd(OrConditionExpression node) => node;
    }
}