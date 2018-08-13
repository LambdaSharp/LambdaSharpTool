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
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool.Internal {

    public class ResolvedImport {

        //--- Fields ---
        public string Key { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class ImportResolver {

        //--- Fields ---
        private readonly IAmazonSimpleSystemsManagement _ssmClient;
        private readonly IDictionary<string, IEnumerable<ResolvedImport>> _resolvedImports = new Dictionary<string, IEnumerable<ResolvedImport>>();
        private IList<string> _imports = new List<string>();
        private IList<string> _missing = new List<string>();

        //--- Constructors ---
        public ImportResolver(IAmazonSimpleSystemsManagement ssmClient) {
            _ssmClient = ssmClient ?? throw new ArgumentNullException(nameof(ssmClient));
        }

        //--- Properties ---
        public IEnumerable<string> MissingImports { get => _missing; }

        //--- Methods ---
        public void Add(string key) => _imports.Add(key);

        public void BatchResolveImports() {
            _missing.Clear();
            _imports = _imports.Distinct().ToList();

            // import by-path parameters
            var importsByPath = _imports.Where(import => import.EndsWith("/")).ToList();
            foreach(var importByPath in importsByPath) {
                _imports.Remove(importByPath);
                var response = _ssmClient.GetAllParametersByPathAsync(importByPath).Result;
                foreach(var parameter in response) {
                    AddResult(parameter.Key, parameter.Value.Value, parameter.Value.Key);

                    // remove every found key from import list, just in case it's listed explicitly
                    _imports.Remove(parameter.Key);
                }
                _resolvedImports[importByPath] = response
                    .OrderBy(t => t.Key)
                    .Select(t => new ResolvedImport {
                        Key = t.Key,
                        Type = t.Value.Key,
                        Value = t.Value.Value
                    })
                    .ToList();
            }

            // import by-key parameters
            for(var importRest = (IEnumerable<string>)_imports; importRest.Any(); importRest = importRest.Skip(10)) {
                var names = importRest.Take(10).ToList();
                var response = _ssmClient.GetParametersAsync(new GetParametersRequest {
                    Names = names
                }).Result;
                foreach(var parameter in response.Parameters) {
                    _imports.Remove(parameter.Name);
                    AddResult(parameter.Name, parameter.Value, parameter.Type);
                }
            }

            // check if any imports are unresolved
            foreach(var import in _imports) {
                _missing.Add(import);
            }
            _imports.Clear();

            // local functions
            void AddResult(string name, string value, string type) {
                _resolvedImports[name] = new List<ResolvedImport> {
                    new ResolvedImport {
                        Key = name,
                        Type = type,
                        Value = value
                    }
                };
            }
        }

        public bool TryGetValue(string key, out string value) {
            if(_resolvedImports.TryGetValue(key, out IEnumerable<ResolvedImport> values) && values.Any()) {
                value = values.First().Value;
                return  true;
            }
            value = null;
            return false;
        }

        public bool TryGetValue(string key, out ResolvedImport value) {
            if(_resolvedImports.TryGetValue(key, out IEnumerable<ResolvedImport> values) && values.Any()) {
                value = values.First();
                return  true;
            }
            value = null;
            return false;
        }

        public bool TryGetValue(string key, out IEnumerable<ResolvedImport> values) {
            return _resolvedImports.TryGetValue(key, out values) && values.Any();
        }
    }
}