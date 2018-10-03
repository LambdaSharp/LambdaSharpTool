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
using MindTouch.LambdaSharp.Tool.Model.AST;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelReferenceResolver : AModelProcessor {

        //--- Constants ---
        private const string SUBVARIABLE_PATTERN = @"\$\{(?!\!)[^\}]+\}";

        //--- Constructors ---
        public ModelReferenceResolver(Settings settings) : base(settings) { }

        //--- Methods ---
        public void Resolve(ModuleNode module) {
            var freeParameters = new Dictionary<string, ParameterNode>();
            var boundParameters = new Dictionary<string, ParameterNode>();

            // resolve all inter-parameter references
            AtLocation("Parameters", () => {
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
                foreach(var parameter in module.Variables.Where(p => p.Resource?.Properties != null)) {
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
                foreach(var parameter in module.Parameters.Where(p => p.Resource?.Properties != null)) {
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
                    AtLocation(output.Name, () => {
                        output.Value = Substitute(output.Value);
                    });
                }
            });

            // resolve references in functions
            AtLocation("Functions", () => {
                foreach(var function in module.Functions) {
                    AtLocation(function.Name, () => {
                        function.Environment = new Dictionary<string, object>(
                            function.Environment.Select(kv => new KeyValuePair<string, object>(kv.Key, Substitute(kv.Value)))
                        );
                        function.VPC = new Dictionary<string, object>(
                            function.VPC.Select(kv => new KeyValuePair<string, object>(kv.Key, Substitute(kv.Value)))
                        );
                    });
                }
            });

            // local functions
            void DiscoverParameters(IEnumerable<ParameterNode> parameters, string prefix = "") {
                if(parameters == null) {
                    return;
                }
                foreach(var parameter in parameters) {
                    if(parameter.Value is string) {
                        freeParameters[prefix + parameter.Name] = parameter;
                    } else if(parameter.Value != null) {
                        boundParameters[prefix + parameter.Name] = parameter;
                    } else if(parameter.Values?.All(value => value is string) == true) {
                        freeParameters[prefix + parameter.Name] = parameter;
                    } else if(parameter.Values != null) {
                        boundParameters[prefix + parameter.Name] = parameter;
                    } else if(parameter.Resource != null) {
                        freeParameters[prefix + parameter.Name] = parameter;
                    }
                    DiscoverParameters(parameter.Parameters, prefix + parameter.Name + "::");
                }
            }

            bool ResolveParameters(IEnumerable<KeyValuePair<string, ParameterNode>> parameters) {
                if(parameters == null) {
                    return false;
                }
                var progress = false;
                foreach(var kv in parameters) {
                    var parameter = kv.Value;
                    AtLocation(parameter.Name, () => {
                        var doesNotContainBoundParameters = true;
                        if(parameter.Value != null) {
                            parameter.Value = Substitute(parameter.Value, CheckBoundParameters);
                        } else if(parameter.Values != null) {
                            parameter.Values = parameter.Values.Select(value => Substitute(value, CheckBoundParameters)).ToList();
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

            void ReportUnresolved(IEnumerable<ParameterNode> parameters, string prefix = "") {
                if(parameters != null) {
                    foreach(var parameter in parameters) {
                        AtLocation(parameter.Name, () => {
                            Substitute(parameter.Value, missingName => {
                                AddError($"circular !Ref dependency on '{missingName}'");
                            });
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
                        if(map.TryGetValue("Ref", out object refObject) && (refObject is string refKey)) {
                            if(TrySubstitute(refKey, out object found)) {
                                return found;
                            }
                            missing?.Invoke(refKey);
                            return value;
                        }
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
                            subPattern = Regex.Replace(subPattern, SUBVARIABLE_PATTERN, match => {
                                var matchText = match.ToString();
                                var name = matchText.Substring(2, matchText.Length - 3).Trim().Split('.', 2);
                                if(!subArgs.ContainsKey(name[0])) {
                                    if(TrySubstitute(name[0], out object found)) {
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
                if(freeParameters.TryGetValue(key, out ParameterNode freeParameter)) {
                    if(freeParameter.Value != null) {
                        found = freeParameter.Value;
                    } else if(freeParameter.Values?.All(v => v is string) == true) {
                        found = string.Join(",", freeParameter.Values);
                    } else if(freeParameter.Values != null) {
                        found = Fn.Join(",", freeParameter.Values.Cast<dynamic>().ToArray());
                    } else {
                        found = Fn.Ref(key.Replace("::", ""));
                    }
                }
                return found != null;
            }
        }
    }
}