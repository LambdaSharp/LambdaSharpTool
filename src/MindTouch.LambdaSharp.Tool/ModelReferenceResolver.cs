/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.Text.RegularExpressions;
using Humidifier;
using MindTouch.LambdaSharp.Tool.Model;
using Newtonsoft.Json;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelReferenceResolver : AModelProcessor {

        //--- Constants ---
        private const string SUBVARIABLE_PATTERN = @"\$\{(?!\!)[^\}]+\}";

        //--- Class Methods ---
        private static void DebugWriteLine(string format) {
#if false
            Console.WriteLine(format);
#endif
        }

        //--- Constructors ---
        public ModelReferenceResolver(Settings settings, string sourceFilename) : base(settings, sourceFilename) { }

        //--- Methods ---
        public void Resolve(Module module) {
            var functionNames = new HashSet<string>(module.Functions.Select(function => function.Name));
            var freeParameters = new Dictionary<string, AParameter>();
            var boundParameters = new Dictionary<string, AParameter>();

            // resolve all inter-parameter references
            AtLocation("Variables", () => {
                DiscoverParameters(module.Parameters);

                // resolve parameter variables via substitution
                while(ResolveParameters(boundParameters.ToList()));

                // report circular dependencies, if any
                ReportUnresolvedParameters(module.Parameters);
                if(Settings.HasErrors) {
                    return;
                }
            });

            // resolve references in input resource properties
            AtLocation("Inputs", () => {
                foreach(var parameter in module.GetAllParameters()
                    .OfType<AInputParameter>()
                    .Where(p => p.Resource?.Properties != null)
                ) {
                    AtLocation(parameter.Name, () => {
                        AtLocation("Resource", () => {
                            AtLocation("Properties", () => {
                                parameter.Resource.Properties = parameter.Resource.Properties.ToDictionary(kv => kv.Key, kv => Substitute(kv.Value, ReportMissingReference));
                            });
                        });
                    });
                }
            });

            // resolve references in resource properties
            AtLocation("Variables", () => {
                foreach(var parameter in module.GetAllParameters()
                    .Where(p => !(p is AInputParameter))
                    .OfType<AResourceParameter>()
                    .Where(p => p.Resource?.Properties != null)
                ) {
                    AtLocation(parameter.Name, () => {
                        AtLocation("Resource", () => {
                            AtLocation("Properties", () => {
                                parameter.Resource.Properties = parameter.Resource.Properties.ToDictionary(kv => kv.Key, kv => Substitute(kv.Value, ReportMissingReference));
                            });
                        });
                    });
                }
            });

            // resolve references in output values
            AtLocation("Outputs", () => {
                foreach(var output in module.Outputs) {
                    switch(output) {
                    case ExportOutput exportOutput:
                        AtLocation(exportOutput.Name, () => {
                            exportOutput.Value = Substitute(exportOutput.Value, ReportMissingReference);
                        });
                        break;
                    case CustomResourceHandlerOutput _:
                    case MacroOutput _:

                        // nothing to do
                        break;
                    default:
                        throw new InvalidOperationException($"cannot resolve references for this type: {output?.GetType()}");
                    }
                }
            });

            // resolve references in functions
            AtLocation("Functions", () => {
                foreach(var function in module.Functions) {
                    AtLocation(function.Name, () => {
                        function.Environment = function.Environment.ToDictionary(kv => kv.Key, kv => Substitute(kv.Value));

                        // update VPC information
                        if(function.VPC != null) {
                            function.VPC.SecurityGroupIds = Substitute(function.VPC.SecurityGroupIds);
                            function.VPC.SubnetIds = Substitute(function.VPC.SubnetIds);
                        }

                        // update function sources
                        foreach(var source in function.Sources) {
                            switch(source) {
                            case AlexaSource alexaSource:
                                if(alexaSource.EventSourceToken != null) {
                                    alexaSource.EventSourceToken = Substitute(alexaSource.EventSourceToken);
                                }
                                break;
                            }

                        }
                    });
                }
            });

            // local functions
            void DiscoverParameters(IEnumerable<AParameter> parameters) {
                if(parameters == null) {
                    return;
                }
                foreach(var parameter in parameters) {
                    switch(parameter) {
                    case ValueParameter valueParameter:
                        if(valueParameter.Reference is string) {
                            freeParameters[parameter.ResourceName] = parameter;
DebugWriteLine($"FREE => {parameter.ResourceName}");
                        } else {
                            boundParameters[parameter.ResourceName] = parameter;
DebugWriteLine($"BOUND => {parameter.ResourceName}");
                        }
                        break;
                    case PackageParameter _:
                    case SecretParameter _:
                    case AInputParameter inputParameter:
                        freeParameters[parameter.ResourceName] = parameter;
DebugWriteLine($"FREE => {parameter.ResourceName}");
                        break;
                    case AResourceParameter resourceParameter:
                        if(resourceParameter.Resource.ResourceReferences.All(value => value is string)) {
                            freeParameters[parameter.ResourceName] = parameter;
DebugWriteLine($"FREE => {parameter.ResourceName}");
                        } else {
                            boundParameters[parameter.ResourceName] = parameter;
DebugWriteLine($"BOUND => {parameter.ResourceName}");
                        }
                        break;
                    default:
                        throw new ApplicationException($"unrecognized parameter type: {parameter?.GetType().ToString() ?? "null"}");
                    }
                    DiscoverParameters(parameter.Parameters);
                }
            }

            bool ResolveParameters(IEnumerable<KeyValuePair<string, AParameter>> parameters) {
                if(parameters == null) {
                    return false;
                }
                var progress = false;
                foreach(var kv in parameters) {

                    // NOTE (2018-10-04, bjorg): each iteration, we loop over a bound variable;
                    //  in the iteration, we attempt to substitute all references with free variables;
                    //  if we do, the variable can be added to the pool of free variables;
                    //  if we iterate over all bound variables without making progress, then we must have
                    //  a circular dependency and we stop.

                    var parameter = kv.Value;
                    AtLocation(parameter.Name, () => {
                        var doesNotContainBoundParameters = true;
                        switch(parameter) {
                        case ValueParameter _:
                            parameter.Reference = Substitute(parameter.Reference, CheckBoundParameters);
                            break;
                        case ReferencedResourceParameter referencedResourceParameter:
                            referencedResourceParameter.Resource.ResourceReferences = referencedResourceParameter.Resource.ResourceReferences.Select(r => Substitute(r, CheckBoundParameters)).ToList();
                            parameter.Reference = FnJoin(",", referencedResourceParameter.Resource.ResourceReferences);
                            break;
                        case CloudFormationResourceParameter cloudFormationResourceParameter:
                            cloudFormationResourceParameter.Resource.Properties = (IDictionary<string, object>)Substitute(cloudFormationResourceParameter.Resource.Properties, CheckBoundParameters);
                            break;
                        default:
                            throw new InvalidOperationException($"cannot resolve references for this type: {parameter?.GetType()}");
                        }
                        if(doesNotContainBoundParameters) {

                            // capture that progress towards resolving all bound variables has been made;
                            // if ever an iteration does not produces progress, we need to stop; otherwise
                            // we will loop forever
                            progress = true;

                            // promote bound variable to free variable
                            freeParameters[parameter.ResourceName] = parameter;
                            boundParameters.Remove(parameter.ResourceName);
DebugWriteLine($"RESOLVED => {parameter.ResourceName} = {Newtonsoft.Json.JsonConvert.SerializeObject(parameter.Reference)}");
                        }

                        // local functions
                        void CheckBoundParameters(string missingName)
                            => doesNotContainBoundParameters = doesNotContainBoundParameters && !boundParameters.ContainsKey(missingName.Replace("::", ""));
                    });
                }
                return progress;
            }

            void ReportUnresolvedParameters(IEnumerable<AParameter> parameters) {
                if(parameters == null) {
                    return;
                }
                foreach(var parameter in parameters) {
                    AtLocation(parameter.Name, () => {
                        switch(parameter) {
                        case ValueParameter valueParameter:
                            Substitute(valueParameter.Reference, ReportMissingReference);
                            break;
                        case ReferencedResourceParameter referencedResourceParameter:
                            foreach(var item in referencedResourceParameter.Resource.ResourceReferences) {
                                Substitute(item, ReportMissingReference);
                            }
                            break;
                        case CloudFormationResourceParameter _:
                        case PackageParameter _:
                        case SecretParameter _:
                        case AInputParameter _:

                            // nothing to do
                            break;
                        default:
                            throw new InvalidOperationException($"cannot check unresolved references for this type: {parameter?.GetType()}");
                        }
                        ReportUnresolvedParameters(parameter.Parameters);
                    });
                }
            }

            void ReportMissingReference(string missingName) {
                if(boundParameters.ContainsKey(missingName)) {
                    AddError($"circular !Ref dependency on '{missingName}'");
                } else {
                    AddError($"could not find !Ref dependency '{missingName}'");
                }
            }

            object Substitute(object value, Action<string> missing = null) {

                // check if we need to convert the dictionary keys to be strings
                if(value is IDictionary<object, object> objectMap) {
                    value = objectMap.ToDictionary(kv => (string)kv.Key, kv => kv.Value);
                }
                switch(value) {
                case IDictionary<string, object> map:
                    map = map.ToDictionary(kv => kv.Key, kv => Substitute(kv.Value, missing));
                    if(map.Count == 1) {

                        // handle !Ref expression
                        if(map.TryGetValue("Ref", out object refObject) && (refObject is string refKey)) {
                            if(TrySubstitute(refKey, null, out object found)) {
                                return found ?? map;
                            }
DebugWriteLine($"NOT FOUND => {refKey}");
                            missing?.Invoke(refKey);
                            return map;
                        }

                        // handle !GetAtt expression
                        if(
                            map.TryGetValue("Fn::GetAtt", out object getAttObject)
                            && (getAttObject is IList<object> getAttArgs)
                            && (getAttArgs.Count == 2)
                            && getAttArgs[0] is string getAttKey
                            && getAttArgs[1] is string getAttAttribute
                        ) {
                            if(TrySubstitute(getAttKey, getAttAttribute, out object found)) {
                                return found ?? map;
                            }
DebugWriteLine($"NOT FOUND => {getAttKey}");
                            missing?.Invoke(getAttKey);
                            return map;
                        }

                        // handle !Sub expression
                        if(map.TryGetValue("Fn::Sub", out object subObject)) {
                            string subPattern;
                            IDictionary<string, object> subArgs = null;

                            // determine which form of !Sub is being used
                            if(subObject is string) {
                                subPattern = (string)subObject;
                                subArgs = new Dictionary<string, object>();
                            } else if(
                                (subObject is IList<object> subList)
                                && (subList.Count == 2)
                                && (subList[0] is string)
                                && (subList[1] is IDictionary<string, object>)
                            ) {
                                subPattern = (string)subList[0];
                                subArgs = (IDictionary<string, object>)subList[1];
                            } else {
                                return map;
                            }

                            // replace as many ${VAR} occurrences as possible
                            var substitions = false;
                            subPattern = Regex.Replace(subPattern, SUBVARIABLE_PATTERN, match => {
                                var matchText = match.ToString();
                                var name = matchText.Substring(2, matchText.Length - 3).Trim().Split('.', 2);
                                if(!subArgs.ContainsKey(name[0])) {
                                    if(TrySubstitute(name[0], (name.Length == 2) ? name[1] : null, out object found)) {
                                        substitions = true;
                                        if(found == null) {
                                            return matchText;
                                        }
                                        if(found is string text) {
                                            return text;
                                        }
                                        var argName = $"P{subArgs.Count}";
                                        subArgs.Add(argName, found);
                                        return "${" + argName + "}";
                                    }
DebugWriteLine($"NOT FOUND => {name[0]}");
                                    missing?.Invoke(name[0]);
                                }
                                return matchText;
                            });
                            if(!substitions) {
                                return map;
                            }

                            // determine which form of !Sub to construct
                            return subArgs.Any()
                                ? FnSub(subPattern, subArgs)
                                : Regex.IsMatch(subPattern, SUBVARIABLE_PATTERN)
                                ? FnSub(subPattern)
                                : subPattern;
                        }
                    }
                    return map;
                case IList<object> list:
                    return list.Select(item => Substitute(item, missing)).ToList();
                case null:
                    AddError("null value is not allowed");
                    return value;
                default:

                    // nothing further to substitute
DebugWriteLine($"FINAL => {value} [{value.GetType()}]");
                    return value;
                }
            }

            bool TrySubstitute(string key, string attribute, out object found) {
                if(key.StartsWith("AWS::", StringComparison.Ordinal)) {
                    found = null;
                    return true;
                }
                key = key.Replace("::", "");

                // TODO (2018-10-30, bjorg): avoid this hack by properly declaring these resources instead
                switch(key) {
                case "ModuleRestApi":
                case "ModuleRestApiStage":
                case "ModuleRestApiAccount":
                    found = (attribute != null)
                        ? FnGetAtt(key, attribute)
                        : FnRef(key);
                    return true;
                default:
                    break;
                }

                // check if key is referring to a function
                if(functionNames.Contains(key)) {
                    found = (attribute != null)
                        ? FnGetAtt(key, attribute)
                        : FnRef(key);
                    return true;
                }

                // see if the requested key can be resolved using a free parameter
                found = null;
                if(freeParameters.TryGetValue(key, out AParameter freeParameter)) {
                    switch(freeParameter) {
                    case ValueParameter _:
                    case SecretParameter _:
                    case PackageParameter _:
                    case ReferencedResourceParameter _:
                    case ValueInputParameter _:
                    case ImportInputParameter _:
                        if(attribute != null) {
                            AddError($"reference '{key}' must resolved to a CloudFormation resource to be used with an Fn::GetAtt expression");
                        }
                        found = freeParameter.Reference;
                        break;
                    case CloudFormationResourceParameter _:
                        found = (attribute != null)
                            ? FnGetAtt(key, attribute)
                            : freeParameter.Reference;
                        break;
                    }
                }
                return found != null;
            }
        }
    }
}