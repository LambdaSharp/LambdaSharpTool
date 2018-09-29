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
using Humidifier;
using MindTouch.LambdaSharp.Tool.Model.AST;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelReferenceResolver : AModelProcessor {

        //--- Constructors ---
        public ModelReferenceResolver(Settings settings) : base(settings) { }

        //--- Methods ---
        public void Resolve(ModuleNode module) {
            var freeParameters = new Dictionary<string, ParameterNode>();
            var boundParameters = new Dictionary<string, ParameterNode>();

            // resolve all inter-parameter references
            AtLocation("Parameters", () => {
                DiscoverParameters(module.Parameters);

                // resolve parameter variables via substitution
                while(ResolveParameters(boundParameters.ToList()));

                // report circular dependencies, if any
                ReportUnresolved(module.Parameters);
                if(Settings.HasErrors) {
                    return;
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

            // resolve references in exported values
            AtLocation("Exports", () => {
                foreach(var export in module.Exports) {
                    AtLocation(export.Name, () => {
                        export.Value = Substitute(export.Value);
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
                if(parameters != null) {
                    foreach(var parameter in parameters) {
                        if(parameter.Value is string) {
                            freeParameters[prefix + parameter.Name] = parameter;
                        } else if(parameter.Value != null) {
                            boundParameters[prefix + parameter.Name] = parameter;
                        } else if(parameter.Values?.All(value => value is string) == true) {
                            freeParameters[prefix + parameter.Name] = parameter;
                        } else if(parameter.Values != null) {
                            boundParameters[prefix + parameter.Name] = parameter;
                        }
                        DiscoverParameters(parameter.Parameters, prefix + parameter.Name + ".");
                    }
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
                            ReportUnresolved(parameter.Parameters, prefix + parameter.Name + ".");
                        });
                    }
                }
            }

            object Substitute(object value, Action<string> missing = null) {
                switch(value) {
                case IDictionary<string, object> map:
                    if((map.Count == 1) && map.TryGetValue("Ref", out object refObject) && (refObject is string refKey)) {
                        if(freeParameters.TryGetValue(refKey, out ParameterNode freeParameter)) {
                            if(freeParameter.Value != null) {
                                return freeParameter.Value;
                            }
                            if(freeParameter.Values.All(v => v is string)) {
                                return string.Join(",", freeParameter.Values);
                            }
                            return Fn.Join(",", freeParameter.Values.Cast<dynamic>().ToArray());
                        }
                        missing?.Invoke(refKey);
                        return value;
                    }
                    return new Dictionary<string, object>(map.Select(kv => new KeyValuePair<string, object>(kv.Key, Substitute(kv.Value, missing))));
                case IList<object> list: {
                        var result = new List<object>();
                        foreach(var item in list) {
                            result.Add(Substitute(item, missing));
                        }
                        return result;
                    }
                default:

                    // nothing further to substitute
                    return value;
                }
            }
        }

    }
}