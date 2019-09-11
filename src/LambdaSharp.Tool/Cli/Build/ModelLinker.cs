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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Cli.Build {
    using static ModelFunctions;

    public class ModelLinker : AModelProcessor {

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
        private ModuleBuilder _builder;
        private Dictionary<string, AModuleItem> _freeItems = new Dictionary<string, AModuleItem>();
        private Dictionary<string, AModuleItem> _boundItems = new Dictionary<string, AModuleItem>();

        //--- Constructors ---
        public ModelLinker(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Process(ModuleBuilder builder) {
            _builder = builder;
            _freeItems.Clear();
            _boundItems.Clear();

            // compute scopes
            AtLocation("Items", () => {
                var functionNames = builder.Items.OfType<FunctionItem>()
                    .Where(function => function.HasWildcardScopedVariables)
                    .Select(function => function.FullName)
                    .ToList();
                foreach(var item in builder.Items) {
                    AtLocation(item.FullName, () => {
                        if(item.Scope.Contains("*") || item.Scope.Contains("all")) {
                            item.Scope = item.Scope

                                // remove wildcard (either '*' or 'all')
                                .Where(scope => (scope != "*") && (scope != "all"))

                                // add all function names, except the one being processed
                                .Union(functionNames)
                                .Where(scope => scope != item.FullName)

                                // nicely organize the scopes
                                .Distinct()
                                .OrderBy(scope => scope)
                                .ToList();
                        }

                        // verify that all defined scope values are valid
                        foreach(var unknownScope in item.Scope.Where(scope => (scope != "public") && !functionNames.Contains(scope))) {
                            LogError($"unknown referenced function '{unknownScope}' in scope definition");
                        }
                    });
                }
            });

            // compute function environments
            AtLocation("Items", () => {
                foreach(var function in builder.Items.OfType<FunctionItem>()) {
                    AtLocation(function.FullName, () => {
                        var environment = function.Function.Environment.Variables;

                        // set default environment variables
                        environment["MODULE_ID"] = FnRef("AWS::StackName");
                        environment["MODULE_INFO"] = builder.Info;
                        environment["LAMBDA_NAME"] = function.FullName;
                        environment["LAMBDA_RUNTIME"] = function.Function.Runtime;
                        environment["DEPLOYMENTBUCKETNAME"] = FnRef("DeploymentBucketName");
                        if(function.HasDeadLetterQueue && _builder.TryGetItem("Module::DeadLetterQueue", out var _))  {
                            environment["DEADLETTERQUEUE"] = FnRef("Module::DeadLetterQueue");
                        }

                        // add all items scoped to this function
                        foreach(var scopeItem in builder.Items.Where(e => e.Scope.Contains(function.FullName))) {
                            var prefix = scopeItem.HasSecretType ? "SEC_" : "STR_";
                            var fullEnvName = prefix + scopeItem.FullName.Replace("::", "_").ToUpperInvariant();

                            // check if item has a condition associated with it
                            environment[fullEnvName] = (dynamic)(
                                ((scopeItem is AResourceItem resourceItem) && (resourceItem.Condition != null))
                                ? FnIf(resourceItem.Condition, scopeItem.GetExportReference(), FnRef("AWS::NoValue"))
                                : scopeItem.GetExportReference()
                            );
                        }

                        // add all explicitly listed environment variables
                        foreach(var kv in function.Environment) {

                            // add explicit environment variable as string value
                            var fullEnvName = "STR_" + kv.Key.Replace("::", "_").ToUpperInvariant();
                            environment[fullEnvName] = (dynamic)kv.Value;
                        }
                    });
                }
            });

            // resolve all inter-item references
            AtLocation("Items", () => {
                DiscoverItems();
                ResolveItems();
                ReportUnresolvedItems();
            });
            if(Settings.HasErrors) {
                return;
            }

            // resolve all references
            builder.VisitAll((item, value) => Substitute(item, value, ReportMissingReference));

            // remove any optional items that are unreachable
            DiscardUnreachableItems();

            // replace all references with their logical IDs
            builder.VisitAll(Finalize);

            // NOTE (2018-12-17, bjorg): at this point, we have to use 'LogicalId' for items

            // check if module contains a finalizer invocation function
            if(
                builder.TryGetItem("Finalizer::Invocation", out var finalizerInvocationItem)
                && (finalizerInvocationItem is ResourceItem finalizerResourceItem)
                && (finalizerResourceItem.Resource is Humidifier.CustomResource finalizerCustomResource)
            ) {
                var allResourceItems = builder.Items
                    .OfType<AResourceItem>()
                    .Where(item => item.FullName != finalizerInvocationItem.FullName)
                    .ToList();

                // finalizer invocation depends on all non-conditional resources
                finalizerResourceItem.DependsOn = allResourceItems
                    .Where(item => item.Condition == null)
                    .Select(item => item.LogicalId)
                    .OrderBy(logicalId => logicalId)
                    .ToList();

                // NOTE: for conditional resources, we need to take a dependency via an expression; however
                //  this approach doesn't work for custom resources because they don't support !Ref
                var allConditionalResourceItems = allResourceItems
                    .Where(item => item.Condition != null)
                    .Where(item => item.HasAwsType)
                    .ToList();
                if(allConditionalResourceItems.Any()) {
                    finalizerCustomResource["DependsOn"] = allConditionalResourceItems
                        .Select(item => FnIf(item.Condition, FnRef(item.LogicalId), FnRef("AWS::NoValue")))
                        .ToList();
                }

            }
            return;

            // local functions
            void DiscoverItems() {
                foreach(var item in builder.Items) {
                    AtLocation(item.FullName, () => {
                        switch(item.Reference) {
                        case null:
                            throw new ApplicationException($"item reference cannot be null: {item.FullName}");
                        case string _:
                            _freeItems[item.FullName] = item;
                            DebugWriteLine(() => $"FREE => {item.FullName}");
                            break;
                        case IList<object> list:
                            if(list.All(value => value is string)) {
                                _freeItems[item.FullName] = item;
                                DebugWriteLine(() => $"FREE => {item.FullName}");
                            } else {
                                _boundItems[item.FullName] = item;
                                DebugWriteLine(() => $"BOUND => {item.FullName}");
                            }
                            break;
                        default:
                            _boundItems[item.FullName] = item;
                            DebugWriteLine(() => $"BOUND => {item.FullName}");
                            break;
                        }
                    });
                }
            }

            void ResolveItems() {
                bool progress;
                do {
                    progress = false;
                    foreach(var item in _boundItems.Values.ToList()) {
                        AtLocation(item.FullName, () => {

                            // NOTE (2018-10-04, bjorg): each iteration, we loop over a bound item;
                            //  in the iteration, we attempt to substitute all references with free items;
                            //  if we do, the item can be added to the pool of free items;
                            //  if we iterate over all bound items without making progress, then we must have
                            //  a circular dependency and we stop.

                            var doesNotContainBoundItems = true;
                            AtLocation("Reference", () => {
                                item.Reference = Substitute(item, item.Reference, (string missingName) => {
                                    doesNotContainBoundItems = doesNotContainBoundItems && !_boundItems.ContainsKey(missingName);
                                });
                            });
                            if(doesNotContainBoundItems) {

                                // capture that progress towards resolving all bound items has been made;
                                // if ever an iteration does not produces progress, we need to stop; otherwise
                                // we will loop forever
                                progress = true;

                                // promote bound item to free item
                                _freeItems[item.FullName] = item;
                                _boundItems.Remove(item.FullName);
                                DebugWriteLine(() => $"RESOLVED => {item.FullName} = {Newtonsoft.Json.JsonConvert.SerializeObject(item.Reference)}");
                            }
                        });
                    }
                } while(progress);
            }

            void ReportUnresolvedItems() {
                foreach(var item in builder.Items) {
                    AtLocation(item.FullName, () => {
                        Substitute(item, item.Reference, ReportMissingReference);
                    });
                }
            }

            void ReportMissingReference(string missingName) {
                if(_boundItems.ContainsKey(missingName)) {
                    LogError($"circular !Ref dependency on '{missingName}'");
                } else {
                    LogError($"could not find '{missingName}'");
                }
            }
        }

        private object Substitute(AModuleItem item, object root, Action<string> missing = null) {
            return Visit(root, value => {

                // handle !Ref expression
                if(TryGetFnRef(value, out var refKey)) {
                    if(TrySubstitute(refKey, null, out var found)) {
                        return found ?? value;
                    }
                    DebugWriteLine(() => $"NOT FOUND => {refKey}");
                    missing?.Invoke(refKey);
                    return value;
                }

                // handle !GetAtt expression
                if(TryGetFnGetAtt(value, out var getAttKey, out var getAttAttribute)) {
                    if(TrySubstitute(getAttKey, getAttAttribute, out var found)) {
                        return found ?? value;
                    }
                    DebugWriteLine(() => $"NOT FOUND => {getAttKey}");
                    missing?.Invoke(getAttKey);
                    return value;
                }

                // handle !Sub expression
                if(TryGetFnSub(value, out var subPattern, out var subArgs)) {

                    // replace as many ${VAR} occurrences as possible
                    var substitions = false;
                    subPattern = ReplaceSubPattern(subPattern, (subRefKey, suffix) => {
                        if(!subArgs.ContainsKey(subRefKey)) {
                            if(TrySubstitute(subRefKey, suffix?.Substring(1), out var found)) {
                                if(found == null) {
                                    return null;
                                }
                                substitions = true;
                                if(found is string text) {
                                    return text;
                                }

                                // substitute found value as new argument
                                var argName = $"P{subArgs.Count}";
                                subArgs.Add(argName, found);
                                return "${" + argName + "}";
                            }
                            DebugWriteLine(() => $"NOT FOUND => {subRefKey}");
                            missing?.Invoke(subRefKey);
                        }
                        return null;
                    });
                    if(!substitions) {
                        return value;
                    }
                    return FnSub(subPattern, subArgs);
                }

                // handle !If expression
                if(TryGetFnIf(value, out var condition, out var ifTrue, out var ifFalse)) {
                    if(condition.StartsWith("@", StringComparison.Ordinal)) {
                        return value;
                    }
                    if(_freeItems.TryGetValue(condition, out var freeItem)) {
                        if(!(freeItem is ConditionItem)) {
                            LogError($"item '{freeItem.FullName}' must be a condition");
                        }
                        return FnIf(freeItem.ResourceName, ifTrue, ifFalse);
                    }
                    DebugWriteLine(() => $"NOT FOUND => {condition}");
                    missing?.Invoke(condition);
                }

                // handle !Condition expression
                if(TryGetFnCondition(value, out condition)) {
                    if(condition.StartsWith("@", StringComparison.Ordinal)) {
                        return value;
                    }
                    if(_freeItems.TryGetValue(condition, out var freeItem)) {
                        if(!(freeItem is ConditionItem)) {
                            LogError($"item '{freeItem.FullName}' must be a condition");
                        }
                        return FnCondition(freeItem.ResourceName);
                    }
                    DebugWriteLine(() => $"NOT FOUND => {condition}");
                    missing?.Invoke(condition);
                }

                // handle !FindInMap expression
                if(TryGetFnFindInMap(value, out var mapName, out var topLevelKey, out var secondLevelKey)) {
                    if(mapName.StartsWith("@", StringComparison.Ordinal)) {
                        return value;
                    }
                    if(_freeItems.TryGetValue(mapName, out AModuleItem freeItem)) {
                        if(!(freeItem is MappingItem)) {
                            LogError($"item '{freeItem.FullName}' must be a mapping");
                        }
                        return FnFindInMap(freeItem.ResourceName, topLevelKey, secondLevelKey);
                    }
                    DebugWriteLine(() => $"NOT FOUND => {mapName}");
                    missing?.Invoke(mapName);
                }
                return value;
            });

            // local functions
            bool TrySubstitute(string key, string attribute, out object found) {
                found = null;
                if(key.StartsWith("AWS::", StringComparison.Ordinal)) {

                    // built-in AWS references can be kept as-is
                    return true;
                } else if(key.StartsWith("@", StringComparison.Ordinal)) {

                    // module resource names must be kept as-is
                    return true;
                }

                // TODO (2019-01-06): must know what types of references are legal (Parameters only -or- Resources and Paramaters)

                // check if the requested key can be resolved using a free item
                if(_freeItems.TryGetValue(key, out var freeItem)) {
                    switch(freeItem) {
                    case ConditionItem _:
                    case MappingItem _:
                    case ResourceTypeItem _:
                        LogError($"item '{freeItem.FullName}' must be a resource, parameter, or variable");
                        break;
                    case ParameterItem _:
                    case VariableItem _:
                    case PackageItem _:
                        if(attribute != null) {
                            LogError($"item '{freeItem.FullName}' does not have attributes");
                        }
                        found = freeItem.Reference;
                        break;
                    case AResourceItem _:

                        // attributes can be used with managed resources/functions
                        if(attribute != null) {
                            if(freeItem.HasTypeValidation && !_builder.HasAttribute(freeItem, attribute)) {
                                LogError($"item '{freeItem.FullName}' of type '{freeItem.Type}' does not have attribute '{attribute}'");
                            }
                            found = FnGetAtt(freeItem.ResourceName, attribute);
                        } else {
                            found = freeItem.Reference;
                        }
                        break;
                    default:
                        throw new ApplicationException($"unsupported type: {freeItem?.GetType().ToString() ?? "<null>"}");
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
                return false;
            }
        }

        private void DiscardUnreachableItems() {
            var reachable = new Dictionary<string, AModuleItem>();
            var found = new Dictionary<string, AModuleItem>();
            var unused = new Dictionary<string, AModuleItem>();
            var foundItemsToRemove = true;
            while(foundItemsToRemove) {
                foundItemsToRemove = false;
                reachable.Clear();
                found.Clear();
                foreach(var item in _builder.Items.OfType<AResourceItem>().Where(res => !res.DiscardIfNotReachable)) {
                    found[item.FullName] = item;
                    item.Visit(FindReachable);
                }
                foreach(var output in _builder.Items.Where(item => item.IsPublic)) {
                    output.Visit(FindReachable);
                }
                foreach(var statement in _builder.ResourceStatements) {
                    FindReachable(null, statement);
                }
                while(found.Any()) {

                    // record found names as reachable
                    foreach(var kv in found) {
                        reachable[kv.Key] = kv.Value;
                    }

                    // detect what is reachable from found item
                    var current = found;
                    found = new Dictionary<string, AModuleItem>();
                    foreach(var kv in current) {
                        kv.Value.Visit(FindReachable);
                    }
                }
                foreach(var item in _builder.Items.ToList()) {
                    if(!reachable.ContainsKey(item.FullName)) {
                        if(item.DiscardIfNotReachable) {
                            foundItemsToRemove = true;
                            DebugWriteLine(() => $"DISCARD '{item.FullName}'");
                            _builder.RemoveItem(item.FullName);
                        } else if(item is ParameterItem) {
                            switch(item.FullName) {
                            case "Secrets":
                            case "XRayTracing":
                            case "DeploymentBucketName":
                            case "DeploymentPrefix":
                            case "DeploymentPrefixLowercase":
                            case "DeploymentRoot":
                            case "DeploymentChecksum":

                                // these are built-in parameters; don't report them
                                break;
                            default:
                                unused[item.FullName] = item;
                                break;
                            }
                        }
                    }
                }
            }
            foreach(var item in unused.Values.OrderBy(e => e.FullName)) {
                LogWarn($"'{item.FullName}' is defined but never used");
            }

            // local functions
            object FindReachable(AModuleItem item, object root) {
                return Visit(root, value => {

                    // handle !Ref expression
                    if(TryGetFnRef(value, out var refKey)) {
                        MarkReachableItem(item, refKey);
                        return value;
                    }

                    // handle !GetAtt expression
                    if(TryGetFnGetAtt(value, out var getAttKey, out var getAttAttribute)) {
                        MarkReachableItem(item, getAttKey);
                        return value;
                    }

                    // handle !Sub expression
                    if(TryGetFnSub(value, out var subPattern, out var subArgs)) {

                        // substitute ${VAR} occurrences if possible
                        subPattern = ReplaceSubPattern(subPattern, (subRefKey, suffix) => {
                            if(!subArgs.ContainsKey(subRefKey)) {
                                MarkReachableItem(item, subRefKey);
                                return "${" + subRefKey.Substring(1) + suffix + "}";
                            }
                            return null;
                        });
                        return value;
                    }

                    // handle !If expression
                    if(TryGetFnIf(value, out var condition, out _, out _)) {
                        MarkReachableItem(item, condition);
                        return value;
                    }

                    // handle !Condition expression
                    if(TryGetFnCondition(value, out condition)) {
                        MarkReachableItem(item, condition);
                        return value;
                    }

                    // handle !FindInMap expression
                    if(TryGetFnFindInMap(value, out var mapName, out _, out _)) {
                        MarkReachableItem(item, mapName);
                        return value;
                    }
                    return value;
                });
            }

            void MarkReachableItem(AModuleItem item, string fullNameOrResourceName) {
                if(fullNameOrResourceName.StartsWith("AWS::", StringComparison.Ordinal)) {
                    return;
                }
                if(_builder.TryGetItem(fullNameOrResourceName, out var refItem)) {
                    if(!reachable.ContainsKey(refItem.FullName)) {
                        if(!found.ContainsKey(refItem.FullName)) {
                            DebugWriteLine(() => $"REACHED {item?.FullName ?? "<null>"} -> {refItem?.FullName ?? "<null>"}");
                        }
                        found[refItem.FullName] = refItem;
                    }
                }
            }
        }

        private object Finalize(AModuleItem item, object root) {
            return Visit(root, value => {

                // handle !Ref expression
                if(TryGetFnRef(value, out var refKey) && refKey.StartsWith("@", StringComparison.Ordinal)) {
                    return FnRef(refKey.Substring(1));
                }

                // handle !GetAtt expression
                if(TryGetFnGetAtt(value, out var getAttKey, out var getAttAttribute) && getAttKey.StartsWith("@", StringComparison.Ordinal)) {
                    return FnGetAtt(getAttKey.Substring(1), getAttAttribute);
                }

                // handle !Sub expression
                if(TryGetFnSub(value, out var subPattern, out var subArgs)) {

                    // replace as many ${VAR} occurrences as possible
                    subPattern = ReplaceSubPattern(subPattern, (subRefKey, suffix) => {
                        if(!subArgs.ContainsKey(subRefKey) && subRefKey.StartsWith("@", StringComparison.Ordinal)) {
                            return "${" + subRefKey.Substring(1) + suffix + "}";
                        }
                        return null;
                    });
                    return FnSub(subPattern, subArgs);
                }

                // handle !If expression
                if(TryGetFnIf(value, out var condition, out var ifTrue, out var ifFalse) && condition.StartsWith("@", StringComparison.Ordinal)) {
                    return FnIf(condition.Substring(1), ifTrue, ifFalse);
                }

                // handle !Condition expression
                if(TryGetFnCondition(value, out condition) && condition.StartsWith("@", StringComparison.Ordinal)) {
                    return FnCondition(condition.Substring(1));
                }

                // handle !FindInMap expression
                if(TryGetFnFindInMap(value, out var mapName, out var topLevelKey, out var secondLevelKey) && mapName.StartsWith("@", StringComparison.Ordinal)) {
                    return FnFindInMap(mapName.Substring(1), topLevelKey, secondLevelKey);
                }
                return value;
            });
        }

        private object Visit(object value, Func<object, object> visitor) {
            switch(value) {
            case IDictionary dictionary: {
                    var map = new Dictionary<string, object>();
                    foreach(DictionaryEntry entry in dictionary) {
                        AtLocation((string)entry.Key, () => {
                            map.Add((string)entry.Key, Visit(entry.Value, visitor));
                        });
                    }
                    var visitedMap = visitor(map);

                    // check if visitor replaced the instance
                    if(!object.ReferenceEquals(visitedMap, map)) {
                        return visitedMap;
                    }

                    // update existing instance in-place
                    foreach(var kv in map) {
                        dictionary[kv.Key] = kv.Value;
                    }
                    return value;
                }
            case IList list: {
                    for(var i = 0; i < list.Count; ++i) {
                        AtLocation($"{i + 1}", () => {
                            list[i] = Visit(list[i], visitor);
                        });
                    }
                    return visitor(value);
                }
            case null:
                LogError("null value is not allowed");
                return value;
            default:
                if(SkipType(value.GetType())) {

                    // nothing further to substitute
                    return value;
                }
                if(value.GetType().FullName.StartsWith("Humidifier.", StringComparison.Ordinal)) {

                    // use reflection to substitute properties
                    foreach(var property in value.GetType().GetProperties().Where(p => !SkipType(p.PropertyType))) {
                        AtLocation(property.Name, () => {
                            object propertyValue;
                            try {
                                propertyValue = property.GetGetMethod()?.Invoke(value, new object[0]);
                            } catch(Exception e) {
                                throw new ApplicationException($"unable to get {value.GetType()}::{property.Name}", e);
                            }
                            if((propertyValue == null) || SkipType(propertyValue.GetType())) {
                                return;
                            }
                            propertyValue = Visit(propertyValue, visitor);
                            try {
                                property.GetSetMethod()?.Invoke(value, new[] { propertyValue });
                            } catch(Exception e) {
                                throw new ApplicationException($"unable to set {value.GetType()}::{property.Name}", e);
                            }
                        });
                    }
                    return visitor(value);
                }
                throw new ApplicationException($"unsupported type: {value.GetType()}");
            }

            // local function
            bool SkipType(Type type) => type.IsValueType || type == typeof(string);
        }
    }
}