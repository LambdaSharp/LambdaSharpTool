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
using YamlDotNet.Serialization;

namespace MindTouch.LambdaSharp.Tool.Model {

    public class Module {

        //--- Properties ---
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public IList<string> Pragmas { get; set; }
        public IList<object> Secrets { get; set; }
        public IList<AParameter> Parameters { get; set; }
        public IList<Function> Functions { get; set; }
        public IList<AOutput> Outputs { get; set; }
        public bool HasModuleRegistration => !HasPragma("no-registration");

        //--- Methods ---
        public bool HasPragma(string pragma) => Pragmas?.Contains(pragma) == true;

        public AParameter GetParameter(string parameterName) {

            // drill down into the parameters collection
            var parts = parameterName.Split("::");
            AParameter current = null;
            var parameters = Parameters;
            foreach(var part in parts) {
                current = parameters?.FirstOrDefault(p => p.Name == part);
                if(current == null) {
                    break;
                }
                parameters = current.Parameters;
            }
            return current ?? throw new KeyNotFoundException(parameterName);
        }

        public IEnumerable<AParameter> GetAllParameters() {
            var stack = new Stack<IEnumerator<AParameter>>();
            stack.Push(Parameters.GetEnumerator());
            try {
                while(stack.Any()) {
                    var top = stack.Peek();
                    if(top.MoveNext()) {
                        yield return top.Current;
                        if(top.Current.Parameters?.Any() == true) {
                            stack.Push(top.Current.Parameters.GetEnumerator());
                        }
                    } else {
                        stack.Pop();
                        try {
                            top.Dispose();
                        } catch { }
                    }
                }
            } finally {
                while(stack.Any()) {
                    var top = stack.Pop();
                    try {
                        top.Dispose();
                    } catch { }
                }
            }
        }
     }
}