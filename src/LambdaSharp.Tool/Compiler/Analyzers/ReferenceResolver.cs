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

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class ReferenceResolver {

        //--- Class Methods ---
        private static void DebugWriteLine(Func<string> lazyMessage) {
#if false
            var text = lazyMessage();
            if(text != null) {
                Console.WriteLine(text);
            }
#endif
        }

        //--- Fields ---
        private readonly Builder _builder;
        private Dictionary<string, AItemDeclaration> _freeDeclarations = new Dictionary<string, AItemDeclaration>();
        private Dictionary<string, AItemDeclaration> _boundDeclarations = new Dictionary<string, AItemDeclaration>();
        private HashSet<AExpression> _freeExpressions = new HashSet<AExpression>();

        //--- Constructors ---
        public ReferenceResolver(Builder builder) => _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        //--- Methods ---
        public void Visit() {
            _freeDeclarations.Clear();
            _boundDeclarations.Clear();
            DiscoverDeclarations();
            ResolveDeclarations();
            ReportUnresolvedDeclarations();
            if(_builder.HasErrors) {
                return;
            }

            // remove declarations that are not reachable and are not required
            DiscardUnreachableDeclarations();
        }

        private void DiscoverDeclarations() {
            foreach(var declaration in _builder.ItemDeclarations) {
                if(declaration.ReferenceExpression is LiteralExpression) {

                    // a literal expression is dependency free
                    _freeDeclarations[declaration.FullName] = declaration;
                    DebugWriteLine(() => $"FREE => {declaration.FullName}");
                } else {
                    _boundDeclarations[declaration.FullName] = declaration;
                    DebugWriteLine(() => $"BOUND => {declaration.FullName}");
                }
            }
        }

        private void ResolveDeclarations() {
            bool progress;
            do {
                progress = false;
                foreach(var item in _boundDeclarations.Values.ToList()) {

                    // NOTE (2018-10-04, bjorg): each iteration, we loop over a bound item;
                    //  in the iteration, we attempt to substitute all references with free items;
                    //  if we do, the item can be added to the pool of free items;
                    //  if we iterate over all bound items without making progress, then we must have
                    //  a circular dependency and we stop.

                    var doesNotContainBoundItems = true;
                    item.ReferenceExpression = Substitute(item, (missingName, _) => {
                        doesNotContainBoundItems = doesNotContainBoundItems && !_boundDeclarations.ContainsKey(missingName);
                    });
                    if(doesNotContainBoundItems) {

                        // capture that progress towards resolving all bound items has been made;
                        // if ever an iteration does not produce progress, we need to stop; otherwise
                        // we will loop forever
                        progress = true;

                        // promote bound item to free item
                        _freeDeclarations[item.FullName] = item;
                        _boundDeclarations.Remove(item.FullName);
                        DebugWriteLine(() => $"RESOLVED => {item.FullName}");
                    }
                }
            } while(progress);
        }

        private void ReportUnresolvedDeclarations() {
            foreach(var declaration in _builder.ItemDeclarations) {
                Substitute(declaration, ReportMissingReference);
            }

            void ReportMissingReference(string missingName, ASyntaxNode node) {
                if(_boundDeclarations.ContainsKey(missingName)) {
                    _builder.Log(Error.ReferenceWithCircularDependency(missingName), node);
                } else {
                    _builder.Log(Error.ReferenceDoesNotExist(missingName), node);
                }
            }
        }

        private void DiscardUnreachableDeclarations() {

            // iterate as long as we find declarations to remove
            bool foundDeclarationsToRemove;
            do {
                foundDeclarationsToRemove = false;
                foreach(var declaration in _builder.ItemDeclarations
                    .Where(declaration => declaration.DiscardIfNotReachable && !declaration.ReverseDependencies.Any())
                    .ToList()
                ) {
                    foundDeclarationsToRemove = true;
                    DebugWriteLine(() => $"DISCARD '{declaration.FullName}'");
                    declaration.UntrackAllDependencies();
                }
            } while(foundDeclarationsToRemove);

            // report unreachable declarations that could not be discarded (such as CloudFormation parameters)
            foreach(var declaration in _builder.ItemDeclarations
                .Where(declaration => !declaration.ReverseDependencies.Any())
                .OrderBy(declaration => declaration.FullName)
            ) {
                _builder.Log(Warning.ReferenceIsUnreachable(declaration.FullName), declaration);
            }
        }

        private AExpression Substitute(AItemDeclaration declaration, Action<string, ASyntaxNode> missing = null) {
            return Visit(declaration.ReferenceExpression, value => {
                switch(value) {
                case ReferenceFunctionExpression referenceFunctionExpression when !_freeExpressions.Contains(value):

                    // attempt to resolve resource reference
                    if(_freeDeclarations.ContainsKey(referenceFunctionExpression.ReferenceName.Value)) {
                        _freeExpressions.Add(value);
                    } else {
                        DebugWriteLine(() => $"NOT FOUND => {referenceFunctionExpression.ReferenceName.Value}");
                        missing?.Invoke(referenceFunctionExpression.ReferenceName.Value, referenceFunctionExpression.ReferenceName);
                    }
                    return value;
                case GetAttFunctionExpression getAttFunctionExpression when !_freeExpressions.Contains(value):

                    // attempt to resolve resource reference
                    if(_freeDeclarations.ContainsKey(getAttFunctionExpression.ReferenceName.Value)) {
                        _freeExpressions.Add(value);
                    } else {
                        DebugWriteLine(() => $"NOT FOUND => {getAttFunctionExpression.ReferenceName.Value}");
                        missing?.Invoke(getAttFunctionExpression.ReferenceName.Value, getAttFunctionExpression.ReferenceName);
                    }
                    return value;
                case ConditionExpression conditionExpression when !_freeExpressions.Contains(value):

                    // attempt to resolve condition reference
                    if(_freeDeclarations.ContainsKey(conditionExpression.ReferenceName.Value)) {
                        _freeExpressions.Add(value);
                    } else {
                        DebugWriteLine(() => $"NOT FOUND => {conditionExpression.ReferenceName.Value}");
                        missing?.Invoke(conditionExpression.ReferenceName.Value, conditionExpression.ReferenceName);
                    }
                    return value;
                case FindInMapFunctionExpression findInMapFunctionExpression when !_freeExpressions.Contains(value):

                    // attempt to resolve mapping reference
                    if(_freeDeclarations.ContainsKey(findInMapFunctionExpression.MapName.Value)) {
                        _freeExpressions.Add(value);
                    } else {
                        DebugWriteLine(() => $"NOT FOUND => {findInMapFunctionExpression.MapName.Value}");
                        missing?.Invoke(findInMapFunctionExpression.MapName.Value, findInMapFunctionExpression.MapName);
                    }
                    return value;
                default:
                    return value;
                }

                // TODO (2019-01-10, bjorg): we need to follow 'Fn::If' expressions to make a better determination

                // // check if we're accessing a conditional resource from a resource with a different condition or no condition
                // var freeItemConditionName = (freeItem as AResourceItem)?.Condition;
                // if((freeItemConditionName != null) && (item is AResourceItem resourceItem)) {
                //     _builder.TryGetItem(freeItemConditionName, out var freeItemCondition);
                //     if(resourceItem.Condition == null) {
                //         LogWarn($"possible reference to conditional item {freeItem.FullName} from non-conditional item");
                //     } else if(resourceItem.Condition != freeItemConditionName) {
                //          _builder.TryGetItem(resourceItem.Condition, out var resourceItemCondition);
                //         LogWarn($"conditional item {freeItem.FullName} with condition '{freeItemCondition?.FullName ?? freeItemConditionName}' is accessed by item with condition '{resourceItemCondition.FullName ?? resourceItem.Condition}'");
                //     }
                // }
            });
        }

        private AExpression Visit(AExpression value, Func<AExpression, AExpression> visitor) {

            // recursively visit all members of each expression and substitute them
            switch(value) {
            case LiteralExpression _:

                // nothing to do
                break;
            case ListExpression listExpression:
                for(var i = 0; i < listExpression.Count; ++i) {
                    listExpression[i] = Visit(listExpression[i], visitor);
                }
                break;
            case ObjectExpression objectExpression:
                foreach(var item in objectExpression) {
                    item.Value = Visit(item.Value, visitor);
                }
                break;
            case Base64FunctionExpression base64FunctionExpression:
                base64FunctionExpression.Value = Visit(base64FunctionExpression.Value, visitor);
                break;
            case CidrFunctionExpression cidrFunctionExpression:
                cidrFunctionExpression.IpBlock = Visit(cidrFunctionExpression.IpBlock, visitor);
                cidrFunctionExpression.Count = Visit(cidrFunctionExpression.Count, visitor);
                cidrFunctionExpression.CidrBits = Visit(cidrFunctionExpression.CidrBits, visitor);
                break;
            case FindInMapFunctionExpression findInMapFunctionExpression:
                findInMapFunctionExpression.TopLevelKey = Visit(findInMapFunctionExpression.TopLevelKey, visitor);
                findInMapFunctionExpression.SecondLevelKey = Visit(findInMapFunctionExpression.SecondLevelKey, visitor);
                break;
            case GetAttFunctionExpression getAttFunctionExpression:
                getAttFunctionExpression.AttributeName = Visit(getAttFunctionExpression.AttributeName, visitor);
                break;
            case GetAZsFunctionExpression getAZsFunctionExpression:
                getAZsFunctionExpression.Region = Visit(getAZsFunctionExpression.Region, visitor);
                break;
            case IfFunctionExpression ifFunctionExpression:
                ifFunctionExpression.Condition = Visit(ifFunctionExpression.Condition, visitor);
                ifFunctionExpression.IfTrue = Visit(ifFunctionExpression.IfTrue, visitor);
                ifFunctionExpression.IfFalse = Visit(ifFunctionExpression.IfFalse, visitor);
                break;
            case ImportValueFunctionExpression importValueFunctionExpression:
                importValueFunctionExpression.SharedValueToImport = Visit(importValueFunctionExpression.SharedValueToImport, visitor);
                break;
            case JoinFunctionExpression joinFunctionExpression:
                joinFunctionExpression.Separator = (LiteralExpression)Visit(joinFunctionExpression.Separator, visitor);
                joinFunctionExpression.Values = Visit(joinFunctionExpression.Values, visitor);
                break;
            case SelectFunctionExpression selectFunctionExpression:
                selectFunctionExpression.Index = Visit(selectFunctionExpression.Index, visitor);
                selectFunctionExpression.Values = Visit(selectFunctionExpression.Values, visitor);
                break;
            case SplitFunctionExpression splitFunctionExpression:
                splitFunctionExpression.Delimiter = (LiteralExpression)Visit(splitFunctionExpression.Delimiter, visitor);
                splitFunctionExpression.SourceString = Visit(splitFunctionExpression.SourceString, visitor);
                break;
            case SubFunctionExpression subFunctionExpression:
                subFunctionExpression.Parameters = (ObjectExpression)Visit(subFunctionExpression.Parameters, visitor);
                break;
            case TransformFunctionExpression transformFunctionExpression:
                if(transformFunctionExpression.Parameters != null) {
                    transformFunctionExpression.Parameters = (ObjectExpression)Visit(transformFunctionExpression.Parameters, visitor);
                }
                break;
            case ReferenceFunctionExpression referenceFunctionExpression:

                // nothing to do
                break;
            case ConditionExpression conditionExpression:

                // nothing to do
                break;
            case EqualsConditionExpression equalsConditionExpression:
                equalsConditionExpression.LeftValue = Visit(equalsConditionExpression.LeftValue, visitor);
                equalsConditionExpression.RightValue = Visit(equalsConditionExpression.RightValue, visitor);
                break;
            case NotConditionExpression notConditionExpression:
                notConditionExpression.Value = Visit(notConditionExpression.Value, visitor);
                break;
            case AndConditionExpression andConditionExpression:
                andConditionExpression.LeftValue = Visit(andConditionExpression.LeftValue, visitor);
                andConditionExpression.RightValue = Visit(andConditionExpression.RightValue, visitor);
                break;
            case OrConditionExpression orConditionExpression:
                orConditionExpression.LeftValue = Visit(orConditionExpression.LeftValue, visitor);
                orConditionExpression.RightValue = Visit(orConditionExpression.RightValue, visitor);
                break;
            case null:
                throw new NullValueException();
            default:
                _builder.Log(Error.UnrecognizedExpressionType(value), value);
                return value;
            }

            // visit item itself
            return visitor(value);
        }
    }
}
