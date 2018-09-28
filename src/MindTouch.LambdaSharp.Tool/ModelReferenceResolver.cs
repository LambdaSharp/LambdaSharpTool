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
using MindTouch.LambdaSharp.Tool.Model.AST;

namespace MindTouch.LambdaSharp.Tool {

    public class ModelReferenceResolver : AModelProcessor {

        //--- Fields ---

        //--- Constructors ---
        public ModelReferenceResolver(Settings settings) : base(settings) { }

        //--- Methods ---
        public void Resolve(ModuleNode module) {
            var freeParameters = new Dictionary<string, ParameterNode>();
            var boundParameters = new Dictionary<string, ParameterNode>();

            // gather all parameter variables
            AtLocation("Parameters", () => {
                foreach(var parameter in module.Parameters) {

                    // TODO (2018-09-28, bjorg): add support for `Values` parmeters

                    if(parameter.Value is string) {
                        freeParameters[parameter.Name] = parameter;
                    } else if(parameter.Value != null) {
                        boundParameters[parameter.Name] = parameter;
                    }
                }

                // resolve parameter variables via substitution
                bool progress;
                do {
                    progress = false;
                    foreach(var parameter in boundParameters.Values.ToList()) {
                        AtLocation(parameter.Name, () => {
                            var clean = true;
                            parameter.Value = Substitute(parameter.Value, _ => clean = false);
                            if(clean) {

                                // capture that progress towards resolving all bound variables has been made;
                                // if ever an iteration does not produces progress, we need to stop; otherwise
                                // we will loop forever
                                progress = true;

                                // promote bound variable to free variable
                                freeParameters[parameter.Name] = parameter;
                                boundParameters.Remove(parameter.Name);
                            }
                        });
                    }

                } while(progress);

                // report any remaining bound variables
                foreach(var parameter in boundParameters) {
                    AtLocation(parameter.Key, () => {
                        Substitute(parameter.Value, missingName => {
                            AddError($"circular !Ref dependency on '{missingName}'");
                        });
                    });
                }
                if(Settings.HasErrors) {
                    return;
                }

                // resolve references in resource properties
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
            object Substitute(object value, Action<string> missing = null) {
                switch(value) {
                case IDictionary<string, object> map:
                    if((map.Count == 1) && map.TryGetValue("Ref", out object refObject) && (refObject is string refKey)) {
                        if(freeParameters.TryGetValue(refKey, out ParameterNode freeParameter)) {
                            return freeParameter.Value;
                        }
                        if(boundParameters.ContainsKey(refKey)) {
                            missing?.Invoke(refKey);
                        }
                    }
                    break;
                case IList<object> list:
                    return list.Select(item => Substitute(item, missing)).ToList();
                default:

                    // nothing further to substitute
                    break;
                }
                return value;
            }
        }

    }
}