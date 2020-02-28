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

using System;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler {

    public abstract class ASyntaxAnalyzer : ISyntaxVisitor {

        //--- Class Methods ---

        #region *** CloudFormation Functions ***
        public static ReferenceFunctionExpression FnRef(string referenceName, bool resolved = false) => new ReferenceFunctionExpression {
            ReferenceName = Literal(referenceName),
            Resolved = resolved
        };

        public static GetAttFunctionExpression FnGetAtt(string referenceName, string attributeName) => new GetAttFunctionExpression {
            ReferenceName = Literal(referenceName),
            AttributeName = Literal(attributeName)
        };

        public static GetAttFunctionExpression FnGetAtt(string referenceName, AExpression attributeName) => new GetAttFunctionExpression {
            ReferenceName = Literal(referenceName),
            AttributeName = attributeName ?? throw new ArgumentNullException(nameof(attributeName))
        };

        public static SubFunctionExpression FnSub(string formatString) => new SubFunctionExpression {
            FormatString = Literal(formatString)
        };

        public static SubFunctionExpression FnSub(string formatString, ObjectExpression parameters) => new SubFunctionExpression {
            FormatString = Literal(formatString),
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters))
        };

        public static JoinFunctionExpression FnJoin(string separator, IEnumerable<AExpression> values) => new JoinFunctionExpression {
            Separator = Literal(separator),
            Values = new ListExpression(values ?? throw new ArgumentNullException(nameof(values))),
        };

        public static SelectFunctionExpression FnSelect(string index, AExpression values) => new SelectFunctionExpression {
            Index = Literal(index),
            Values = values ?? throw new ArgumentNullException(nameof(values))
        };

        public static SplitFunctionExpression FnSplit(string delimiter, AExpression sourceString) => new SplitFunctionExpression {
            Delimiter = Literal(delimiter),
            SourceString = sourceString ?? throw new ArgumentNullException(nameof(sourceString))
        };

        public static FindInMapFunctionExpression FnFindInMap(string mapName, AExpression topLevelKey, AExpression secondLevelKey) => new FindInMapFunctionExpression {
            MapName = Literal(mapName),
            TopLevelKey = topLevelKey ?? throw new ArgumentNullException(nameof(topLevelKey)),
            SecondLevelKey = secondLevelKey ?? throw new ArgumentNullException(nameof(secondLevelKey))
        };

        public static ImportValueFunctionExpression FnImportValue(AExpression sharedValueToImport) => new ImportValueFunctionExpression {
            SharedValueToImport = sharedValueToImport ?? throw new ArgumentNullException(nameof(sharedValueToImport))
        };

        public static IfFunctionExpression FnIf(string condition, AExpression ifTrue, AExpression ifFalse) => new IfFunctionExpression {
            Condition = FnCondition(condition),
            IfTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue)),
            IfFalse = ifFalse ?? throw new ArgumentNullException(nameof(ifFalse))
        };

        // TODO: consider inlining to allow SourceLocation to be set more easily
        public static LiteralExpression Literal(string value) => new LiteralExpression(value);

        // TODO: consider inlining to allow SourceLocation to be set more easily
        public static LiteralExpression Literal(int value) => new LiteralExpression(value);

        public static ListExpression LiteralList(params string[] values) => new ListExpression(values.Select(value => Literal(value)));

        public static NotConditionExpression FnNot(AExpression condition) => new NotConditionExpression {
            Value = condition ?? throw new ArgumentNullException(nameof(condition))
        };

        public static EqualsConditionExpression FnEquals(AExpression leftValue, AExpression rightValue) => new EqualsConditionExpression {
            LeftValue = leftValue ?? throw new ArgumentNullException(nameof(leftValue)),
            RightValue = rightValue ?? throw new ArgumentNullException(nameof(rightValue))
        };

        public static AndConditionExpression FnAnd(AExpression leftValue, AExpression rightValue) => new AndConditionExpression {
            LeftValue = leftValue ?? throw new ArgumentNullException(nameof(leftValue)),
            RightValue = rightValue ?? throw new ArgumentNullException(nameof(rightValue))
        };

        public static OrConditionExpression FnOr(AExpression leftValue, AExpression rightValue) => new OrConditionExpression {
            LeftValue = leftValue ?? throw new ArgumentNullException(nameof(leftValue)),
            RightValue = rightValue ?? throw new ArgumentNullException(nameof(rightValue))
        };

        public static ConditionExpression FnCondition(string referenceName) => new ConditionExpression {
            ReferenceName = Literal(referenceName)
        };
        #endregion

        //--- Methods ---
        public virtual void VisitStart(ASyntaxNode? parent, ModuleDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ModuleDeclaration node) => node;
        public void VisitStart(ASyntaxNode? parent, ModuleDeclaration.CloudFormationSpecExpression node) { }
        public ASyntaxNode? VisitEnd(ASyntaxNode? parent, ModuleDeclaration.CloudFormationSpecExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, UsingModuleDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, UsingModuleDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ApiEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ApiEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, SchedulEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, SchedulEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, S3EventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, S3EventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, SlackCommandEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, SlackCommandEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, TopicEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, TopicEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, SqsEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, SqsEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, AlexaEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, AlexaEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, DynamoDBEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, DynamoDBEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, KinesisEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, KinesisEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, WebSocketEventSourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, WebSocketEventSourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, Base64FunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, Base64FunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, CidrFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, CidrFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, FindInMapFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, FindInMapFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, GetAttFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, GetAttFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, GetAZsFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, GetAZsFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, IfFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, IfFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ImportValueFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ImportValueFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, JoinFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, JoinFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, SelectFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, SelectFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, SplitFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, SplitFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, SubFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, SubFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, TransformFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, TransformFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ReferenceFunctionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ReferenceFunctionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ParameterDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ParameterDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, PseudoParameterDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, PseudoParameterDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ImportDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ImportDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, VariableDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, VariableDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, GroupDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, GroupDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ConditionDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ConditionDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ResourceDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ResourceDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, NestedModuleDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, NestedModuleDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, PackageDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, PackageDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, FunctionDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, FunctionDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, FunctionDeclaration.VpcExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, FunctionDeclaration.VpcExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, MappingDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, MappingDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ResourceTypeDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ResourceTypeDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ResourceTypeDeclaration.PropertyTypeExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ResourceTypeDeclaration.PropertyTypeExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ResourceTypeDeclaration.AttributeTypeExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ResourceTypeDeclaration.AttributeTypeExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, MacroDeclaration node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, MacroDeclaration node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ObjectExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ObjectExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ListExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ListExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, LiteralExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, LiteralExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, ConditionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, ConditionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, EqualsConditionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, EqualsConditionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, NotConditionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, NotConditionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, AndConditionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, AndConditionExpression node) => node;
        public virtual void VisitStart(ASyntaxNode? parent, OrConditionExpression node) { }
        public virtual ASyntaxNode? VisitEnd(ASyntaxNode? parent, OrConditionExpression node) => node;
    }
}