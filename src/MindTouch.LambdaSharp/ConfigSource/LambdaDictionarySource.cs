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

namespace MindTouch.LambdaSharp.ConfigSource {

    public sealed class LambdaDictionarySource : ILambdaConfigSource {

        private static string NormalizeKey(string key)
            => (key.Any() && key.All(char.IsLetterOrDigit))
                ? key
                : throw new LambdaConfigIllegalKeyException(key);

        //--- Fields ---
        private readonly string _path;
        private readonly IDictionary<string, string> _parameters;

        //--- Constructors ---
        public LambdaDictionarySource(IDictionary<string, string> parameters) : this("", parameters) {}

        public LambdaDictionarySource(string path, IDictionary<string, string> parameters) {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _parameters = new Dictionary<string, string>(
                parameters ?? throw new ArgumentNullException(nameof(parameters)),
                StringComparer.InvariantCultureIgnoreCase
            );
        }

        //--- Methods ---
        public ILambdaConfigSource Open(string key)
            => new LambdaDictionarySource(CombinePathWithKey(key), _parameters);

        public string Read(string key) {
            var keyPath = CombinePathWithKey(key);
            if(_parameters.TryGetValue(keyPath, out var result)) {
                return result;
            }
            return null;
        }

        public IEnumerable<string> ReadAllKeys() {
            var subpath = CombinePathWithKey("");
            return _parameters.Keys
                .Select(ExtractSubKey)
                .Where(key => key != null)
                .Distinct()
                .ToArray();

            // local functions
            string ExtractSubKey(string key) {

                // key doesn't match the prefix
                if(!key.StartsWith(subpath, StringComparison.Ordinal)) {
                    return null;
                }

                // drop the prefix and only keep what comes after
                var subkey = key.Substring(subpath.Length);

                // check if we need to remove a suffix as well (starting at '/')
                var indexOfUnderscore = subkey.IndexOf('/');
                if(indexOfUnderscore >= 0) {
                    subkey = subkey.Substring(0, indexOfUnderscore);
                }
                return subkey;
            }
        }

        private string CombinePathWithKey(string key) => _path + "/" +  NormalizeKey(key);
    }
}