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

namespace MindTouch.LambdaSharp.Tool {

    public class ModelReferenceResolver : AModelProcessor {

        //--- Constants ---
        private const string SUBVARIABLE_PATTERN = @"\$\{(?!\!)[^\}]+\}";

        //--- Constructors ---
        public ModelReferenceResolver(Settings settings) : base(settings) { }

        //--- Methods ---
        public void Resolve(Module module) {
            var freeInputs = new Dictionary<string, Input>();
            var freeParameters = new Dictionary<string, AParameter>();
            var boundParameters = new Dictionary<string, AParameter>();

            // resolve all inter-parameter references
            AtLocation("Parameters", () => {
                DiscoverInputs(module.Inputs);
                DiscoverParameters(module.Variables);
                DiscoverParameters(module.Parameters);

                // resolve parameter variables via substitution
                while(ResolveParameters(boundParameters.ToList()));

                // report circular dependencies, if any
                ReportUnresolved(module.Variables);
                ReportUnresolved(module.Parameters);
                if(Settings.HasErrors) {
                    return;
                }
            });

            // resolve references in resource properties
            AtLocation("Variables", () => {
                foreach(var parameter in module.Variables.OfType<AResourceParameter>().Where(p => p.Resource?.Properties != null)) {
                    AtLocation(parameter.Name, () => {
                        AtLocation("Resource", () => {
                            AtLocation("Properties", () => {
                                parameter.Resource.Properties = new Dictionary<string, object>(
                                    parameter.Resource.Properties.Select(kv => new KeyValuePair<string, object>(kv.Key, Substitute(kv.Value)))
                                );
                            });
                        });
                    });
                }
            });

            // resolve references in resource properties
            AtLocation("Parameters", () => {
                foreach(var parameter in module.Parameters.OfType<AResourceParameter>().Where(p => p.Resource?.Properties != null)) {
                    AtLocation(parameter.Name, () => {
                        AtLocation("Resource", () => {
                            AtLocation("Properties", () => {
                                parameter.Resource.Properties = new Dictionary<string, object>(
                                    parameter.Resource.Properties.Select(kv => new KeyValuePair<string, object>(kv.Key, Substitute(kv.Value)))
                                );
                            });
                        });
                    });
                }
            });

            // resolve references in output values
            AtLocation("Outputs", () => {
                foreach(var output in module.Outputs) {
                    switch(output) {
                    case StackOutput stackOutput:
                        AtLocation(stackOutput.Name, () => {
                            stackOutput.Value = Substitute(stackOutput.Value);
                        });
                        break;
                    case ExportOutput exportOutput:
                        AtLocation(exportOutput.Name, () => {
                            exportOutput.Value = Substitute(exportOutput.Value);
                        });
                        break;
                    case CustomResourceHandlerOutput customResourceHandlerOutput:

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
                        function.Environment = new Dictionary<string, object>(
                            function.Environment.Select(kv => new KeyValuePair<string, object>(kv.Key, Substitute(kv.Value)))
                        );
                        if(function.VPC != null) {
                            function.VPC.SecurityGroupIds = Substitute(function.VPC.SecurityGroupIds);
                            function.VPC.SubnetIds = Substitute(function.VPC.SubnetIds);
                        }
                    });
                }
            });

            // local functions
            void DiscoverInputs(IEnumerable<Input> inputs) {
                foreach(var input in inputs) {
                    freeInputs[input.Name] = input;
                }
            }

            void DiscoverParameters(IEnumerable<AParameter> parameters, string prefix = "") {
                if(parameters == null) {
                    return;
                }
                foreach(var parameter in parameters) {
                    switch(parameter) {
                    case ValueParameter valueParameter:
                        if(valueParameter.Value is string) {
                            freeParameters[prefix + parameter.Name] = parameter;
                        } else {
                            boundParameters[prefix + parameter.Name] = parameter;
                        }
                        break;
                    case ValueListParameter listParameter:
                        if(listParameter.Values.All(value => value is string)) {
                            freeParameters[prefix + parameter.Name] = parameter;
                        } else {
                            boundParameters[prefix + parameter.Name] = parameter;
                        }
                        break;
                    case ReferencedResourceParameter referencedParameter:
                        if(referencedParameter.Resource.ResourceReferences.All(value => value is string)) {
                            freeParameters[prefix + parameter.Name] = parameter;
                        } else {
                            boundParameters[prefix + parameter.Name] = parameter;
                        }
                        break;
                    case CloudFormationResourceParameter _:
                        freeParameters[prefix + parameter.Name] = parameter;
                        break;

                        // TODO (2018-10-03, bjorg): what about `SecretParameter` and `PackageParameter`?
                    }
                    DiscoverParameters(parameter.Parameters, prefix + parameter.Name + "::");
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
                        case ValueParameter valueParameter:
                            valueParameter.Value = Substitute(valueParameter.Value, CheckBoundParameters);
                            break;
                        case ValueListParameter listParameter:
                            listParameter.Values = listParameter.Values.Select(value => Substitute(value, CheckBoundParameters)).ToList();
                            break;
                        case ReferencedResourceParameter referencedParameter:
                            referencedParameter.Resource.ResourceReferences = referencedParameter.Resource.ResourceReferences.Select(value => Substitute(value, CheckBoundParameters)).ToList();
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
                            freeParameters[kv.Key] = parameter;
                            boundParameters.Remove(kv.Key);
                        }

                        // local functions
                        void CheckBoundParameters(string missingName)
                            => doesNotContainBoundParameters = doesNotContainBoundParameters && !boundParameters.ContainsKey(missingName);
                    });
                }
                return progress;
            }

            void ReportUnresolved(IEnumerable<AParameter> parameters, string prefix = "") {
                if(parameters != null) {
                    foreach(var parameter in parameters) {
                        AtLocation(parameter.Name, () => {
                            switch(parameter) {
                            case ValueParameter valueParameter:
                                Substitute(valueParameter.Value, missingName => {
                                    AddError($"circular !Ref dependency on '{missingName}'");
                                });
                                break;
                            case ValueListParameter listParameter:
                                foreach(var item in listParameter.Values) {
                                    Substitute(item, missingName => {
                                        AddError($"circular !Ref dependency on '{missingName}'");
                                    });
                                }
                                break;
                            }
                            ReportUnresolved(parameter.Parameters, prefix + parameter.Name + "::");
                        });
                    }
                }
            }

            object Substitute(object value, Action<string> missing = null) {
                switch(value) {
                case IDictionary<string, object> map:
                    map = new Dictionary<string, object>(map.Select(kv => new KeyValuePair<string, object>(kv.Key, Substitute(kv.Value, missing))));
                    if(map.Count == 1) {

                        // handle !Ref expression
                        if(map.TryGetValue("Ref", out object refObject) && (refObject is string refKey)) {
                            if(TrySubstitute(refKey, out object found)) {
                                return found;
                            }
                            missing?.Invoke(refKey);
                            return value;
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
                                    if(TrySubstitute(name[0], out object found)) {
                                        substitions = true;
                                        if(found is string text) {
                                            if(name.Length == 2) {
                                                AddError($"reference '{name[0]}' resolved to a literal value, but is used in a Fn::GetAtt expression");
                                            }
                                            return text;
                                        }
                                        var argName = $"P{subArgs.Count}";
                                        if(name.Length == 2) {
                                            found = new Dictionary<string, object> {
                                                ["Fn::GetAtt"] = new List<object> {
                                                    found,
                                                    name[1]
                                                }
                                            };
                                        }
                                        subArgs.Add(argName, found);
                                        return "${" + argName + "}";
                                    } else {
                                        missing?.Invoke(name[0]);
                                    }
                                }
                                return matchText;
                            });
                            if(!substitions) {
                                return map;
                            }

                            // determine which form of !Sub to construct
                            if(subArgs.Count == 0) {

                                // check if !Sub pattern still contains any variables
                                if(Regex.IsMatch(subPattern, SUBVARIABLE_PATTERN)) {
                                    return new Dictionary<string, object> {
                                        ["Fn::Sub"] = subPattern
                                    };
                                } else {
                                    return subPattern;
                                }
                            } else {
                                return new Dictionary<string, object> {
                                    ["Fn::Sub"] = new List<object> {
                                        subPattern,
                                        subArgs
                                    }
                                };
                            }
                        }
                    }
                    return map;
                case IList<object> list:
                    return list.Select(item => Substitute(item, missing)).ToList();
                default:

                    // nothing further to substitute
                    return value;
                }
            }

            bool TrySubstitute(string key, out object found) {
                found = null;
                if(freeInputs.TryGetValue(key, out Input freeInput)) {
                    found = freeInput.Reference;
                } else if(freeParameters.TryGetValue(key, out AParameter freeParameter)) {
                    switch(freeParameter) {
                    case ValueParameter valueParameter:
                        found = valueParameter.Value;
                        break;
                    case ValueListParameter listParameter:
                        if(listParameter.Values.All(value => value is string)) {
                            found = string.Join(",", listParameter.Values);
                        } else {
                            found = Fn.Join(",", listParameter.Values.Cast<dynamic>().ToArray());
                        }
                        break;
                    case ReferencedResourceParameter referencedParameter:
                        if(referencedParameter.Resource.ResourceReferences.All(value => value is string)) {
                            found = string.Join(",", referencedParameter.Resource.ResourceReferences);
                        } else {
                            found = Fn.Join(",", referencedParameter.Resource.ResourceReferences.Cast<dynamic>().ToArray());
                        }
                        break;
                    case CloudFormationResourceParameter _:
                        found = Fn.Ref(key.Replace("::", ""));
                        break;

                        // TODO (2018-10-03, bjorg): what about `SecretParameter` and `PackageParameter`?
                    }
                }
                return found != null;
            }
        }
    }
}