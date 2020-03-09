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

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    // TODO: rename class; this class is about iterating through declarations and resolving any dependencies
    public class ResolveReferences {

        //--- Types ---
        private class Analyzer : ASyntaxAnalyzer {

            //--- Fields ---
            private readonly ResolveReferences _logic;
            private readonly AItemDeclaration _declaration;
            private readonly Action<string, ASyntaxNode> _missing;

            //--- Constructors ---
            public Analyzer(ResolveReferences logic, AItemDeclaration declaration, Action<string, ASyntaxNode> missing) {
                _logic = logic ?? throw new ArgumentNullException(nameof(logic));
                _declaration = declaration;
                _missing = missing;
            }

            //--- Methods ---
            public override bool VisitStart(ReferenceFunctionExpression node) {

                // attempt to resolve resource reference
                if(!_logic._freeDeclarations.ContainsKey(node.ReferenceName.Value)) {
                    _logic.DebugWriteLine(() => _logic._boundDeclarations.ContainsKey(node.ReferenceName.Value) ? null : $"NOT FOUND => {node.ReferenceName.Value}");
                    _missing?.Invoke(node.ReferenceName.Value, node.ReferenceName);
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
                return true;
            }

            public override bool VisitStart(GetAttFunctionExpression node) {

                // attempt to resolve resource reference
                if(!_logic._freeDeclarations.ContainsKey(node.ReferenceName.Value)) {
                    _logic.DebugWriteLine(() => _logic._boundDeclarations.ContainsKey(node.ReferenceName.Value) ? null : $"NOT FOUND => {node.ReferenceName.Value}");
                    _missing?.Invoke(node.ReferenceName.Value, node.ReferenceName);
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
                return true;
            }

            public override bool VisitStart(ConditionExpression node) {

                // attempt to resolve condition reference
                if(!_logic._freeDeclarations.ContainsKey(node.ReferenceName.Value)) {
                    _logic.DebugWriteLine(() => _logic._boundDeclarations.ContainsKey(node.ReferenceName.Value) ? null : $"NOT FOUND => {node.ReferenceName.Value}");
                    _missing?.Invoke(node.ReferenceName.Value, node.ReferenceName);
                }
                return true;
            }

            public override bool VisitStart(FindInMapFunctionExpression node) {

                // attempt to resolve mapping reference
                if(!_logic._freeDeclarations.ContainsKey(node.MapName.Value)) {
                    _logic.DebugWriteLine(() => _logic._boundDeclarations.ContainsKey(node.MapName.Value) ? null : $"NOT FOUND => {node.MapName.Value}");
                    _missing?.Invoke(node.MapName.Value, node.MapName);
                }
                return true;
            }

            public override bool VisitStart(ParameterDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(PseudoParameterDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(ImportDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(VariableDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(GroupDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(ConditionDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(ResourceDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(NestedModuleDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(PackageDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(FunctionDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(FunctionDeclaration.VpcExpression node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(MappingDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(ResourceTypeDeclaration node) => object.ReferenceEquals(node, _declaration);
            public override bool VisitStart(MacroDeclaration node) => object.ReferenceEquals(node, _declaration);
        }

        //--- Fields ---
        private readonly Builder _builder;
        private Dictionary<string, AItemDeclaration> _freeDeclarations = new Dictionary<string, AItemDeclaration>();
        private Dictionary<string, AItemDeclaration> _boundDeclarations = new Dictionary<string, AItemDeclaration>();

        //--- Constructors ---
        public ResolveReferences(Builder builder) => _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        //--- Methods ---
        public void Resolve(ModuleDeclaration module) {
            _freeDeclarations.Clear();
            _boundDeclarations.Clear();
            DiscoverDeclarations();
            ResolveDeclarations();
            ReportUnresolvedDeclarations(module);

            // remove declarations that are not reachable and are not required
            DiscardUnreachableDeclarations();
        }

        private void DiscoverDeclarations() {
            foreach(var declaration in _builder.ItemDeclarations) {
                if(declaration.ReferenceExpression is LiteralExpression) {

                    // a literal expression is dependency free
                    _freeDeclarations[declaration.FullName] = declaration;
                    DebugWriteLine(() => $"FREE => {declaration.FullName} [{declaration.GetType().Name}]");
                } else {
                    _boundDeclarations[declaration.FullName] = declaration;
                    DebugWriteLine(() => $"BOUND => {declaration.FullName} [{declaration.GetType().Name}]");
                }
            }
        }

        private void ResolveDeclarations() {
            bool progress;
            do {
                DebugWriteLine(() => "RESOLVING...");
                progress = false;
                foreach(var item in new List<AItemDeclaration>(_boundDeclarations.Values)) {

                    // NOTE (2018-10-04, bjorg): each iteration, we loop over a bound item;
                    //  in the iteration, we attempt to substitute all references with free items;
                    //  if we do, the item can be added to the pool of free items;
                    //  if we iterate over all bound items without making progress, then we must have
                    //  a circular dependency and we stop.

                    var doesNotContainBoundItems = true;
                    item.Visit(new Analyzer(this, item, (missingName, node) => {
                        DebugWriteLine(() => $"BOUND REF => {item.FullName} -> {missingName}");
                        doesNotContainBoundItems = false;
                    }));
                    if(doesNotContainBoundItems) {

                        // capture that progress towards resolving all bound items has been made;
                        // if an iteration does not produce progress, we need to stop; otherwise
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

        private void ReportUnresolvedDeclarations(ModuleDeclaration module) {
            module.Visit(new Analyzer(this, declaration: null, (missingName, node) => {
                if(_boundDeclarations.TryGetValue(missingName, out var declaration)) {
                    _builder.Log(Error.ReferenceWithCircularDependency(missingName), node);
                } else {
                    _builder.Log(Error.ReferenceDoesNotExist(missingName), node);
                }
            }));
        }

        private void DiscardUnreachableDeclarations() {

            // iterate as long as we find declarations to remove
            bool foundDeclarationsToRemove;
            do {
                foundDeclarationsToRemove = false;
                foreach(var declaration in new List<AItemDeclaration>(
                    _builder.ItemDeclarations.Where(declaration => declaration.DiscardIfNotReachable && !declaration.ReverseDependencies.Any())
                )) {
                    foundDeclarationsToRemove = true;
                    DebugWriteLine(() => $"DISCARD '{declaration.FullName}'");
                    _builder.RemoveItemDeclaraion(declaration);
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

        private void DebugWriteLine(Func<string> lazyMessage) {

            // TODO: check if logging mode allow debug messages
#if true
            var text = lazyMessage();
            if(text != null) {
                _builder.Log(new Debug(text));
            }
#endif
        }
    }
}
