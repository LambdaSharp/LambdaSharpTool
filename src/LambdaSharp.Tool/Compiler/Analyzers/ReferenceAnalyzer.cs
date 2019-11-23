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
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class ReferenceAnalyzer : ASyntaxAnalyzer {

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public ReferenceAnalyzer(Builder builder) => _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, ListExpression node)  {

            // NOTE (2019-11-07, bjorg): by this stage, list expressions should only contain values as
            //  condition function have already been converted into the their respective class instances.
            foreach(var item in node) {
                ArgumentIsValue(item);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ObjectExpression node)  {

            // NOTE (2019-11-07, bjorg): by this stage, object expressions should only contain values as
            //  condition function have already been converted into the their respective class instances.
            foreach(var item in node) {
                ArgumentIsValue(item.Key);
                ArgumentIsValue(item.Value);
            }
        }

        public override void VisitStart(ASyntaxNode parent, Base64FunctionExpression node) {
            ArgumentIsValue(node.Value);
        }

        public override void VisitStart(ASyntaxNode parent, CidrFunctionExpression node) {
            ArgumentIsValue(node.IpBlock);
            ArgumentIsValue(node.Count);
            ArgumentIsValue(node.CidrBits);
        }

        public override void VisitStart(ASyntaxNode parent, FindInMapFunctionExpression node) {
            ArgumentIsValue(node.MapName);
            ArgumentIsValue(node.TopLevelKey);
            ArgumentIsValue(node.SecondLevelKey);
        }

        public override void VisitStart(ASyntaxNode parent, GetAttFunctionExpression node) {
            ArgumentIsValue(node.ReferenceName);
            ArgumentIsValue(node.AttributeName);
        }

        public override void VisitStart(ASyntaxNode parent, GetAZsFunctionExpression node) {
            ArgumentIsValue(node.Region);
        }

        public override void VisitStart(ASyntaxNode parent, IfFunctionExpression node) {
            ArgumentIsCondition(node.Condition);
            ArgumentIsValue(node.IfTrue);
            ArgumentIsValue(node.IfFalse);
        }

        public override void VisitStart(ASyntaxNode parent, ImportValueFunctionExpression node) {
            ArgumentIsValue(node.SharedValueToImport);
        }

        public override void VisitStart(ASyntaxNode parent, JoinFunctionExpression node) {
            ArgumentIsValue(node.Separator);
            ArgumentIsValue(node.Values);
        }

        public override void VisitStart(ASyntaxNode parent, SelectFunctionExpression node) {
            ArgumentIsValue(node.Index);
            ArgumentIsValue(node.Values);
        }

        public override void VisitStart(ASyntaxNode parent, SplitFunctionExpression node) {
            ArgumentIsValue(node.Delimiter);
            ArgumentIsValue(node.SourceString);
        }

        public override void VisitStart(ASyntaxNode parent, SubFunctionExpression node) {
            ArgumentIsValue(node.FormatString);
            if(node.Parameters != null) {
                ArgumentIsValue(node.Parameters);
            }
        }

        public override void VisitStart(ASyntaxNode parent, TransformFunctionExpression node) {
            ArgumentIsValue(node.MacroName);
            if(node.Parameters != null) {
                ArgumentIsValue(node.Parameters);
            }
        }

        public override void VisitStart(ASyntaxNode parent, ReferenceFunctionExpression node) {
            ArgumentIsValue(node.ReferenceName);
        }

        public override void VisitStart(ASyntaxNode parent, ConditionExpression node) {
            ArgumentIsValue(node.ReferenceName);
        }

        public override void VisitStart(ASyntaxNode parent, EqualsConditionExpression node) {
            ArgumentIsValue(node.LeftValue);
            ArgumentIsValue(node.RightValue);
        }

        public override void VisitStart(ASyntaxNode parent, NotConditionExpression node) {
            ArgumentIsCondition(node.Value);
        }

        public override void VisitStart(ASyntaxNode parent, AndConditionExpression node) {
            ArgumentIsCondition(node.LeftValue);
            ArgumentIsCondition(node.RightValue);
        }

        public override void VisitStart(ASyntaxNode parent, OrConditionExpression node) {
            ArgumentIsCondition(node.LeftValue);
            ArgumentIsCondition(node.RightValue);
        }

        private void ArgumentIsCondition(AExpression expression) {
            switch(expression) {
            case ConditionExpression _:
            case NotConditionExpression _:
            case AndConditionExpression _:
            case OrConditionExpression _:

                // nothing to do
                break;
            case TransformFunctionExpression _:

                // TODO (2019-11-07, bjorg): we need more information about the CloudFormation macro to determine the outcome of the tranform function

                // nothing we can do
                break;
            default:
                _builder.Log(Error.ExpectedConditionExpression, expression);
                break;
            }
        }

        private void ArgumentIsValue(AExpression expression) {
            switch(expression) {
            case ListExpression _:
            case ObjectExpression _:
            case LiteralExpression _:
            case Base64FunctionExpression _:
            case CidrFunctionExpression _:
            case FindInMapFunctionExpression _:
            case GetAttFunctionExpression _:
            case GetAZsFunctionExpression _:
            case IfFunctionExpression _:
            case ImportValueFunctionExpression _:
            case JoinFunctionExpression _:
            case SelectFunctionExpression _:
            case SplitFunctionExpression _:
            case SubFunctionExpression _:
            case ReferenceFunctionExpression _:
            case EqualsConditionExpression _:

                // nothing to do
                break;
            case TransformFunctionExpression _:

                // TODO (2019-11-07, bjorg): we need more information about the CloudFormation macro to determine the outcome of the tranform function

                // nothing we can do
                break;
            default:
                 _builder.Log(Error.ExpectedConditionExpression, expression);
                break;
           }
        }
    }
}