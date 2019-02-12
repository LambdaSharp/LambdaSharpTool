/*
 * MindTouch λ#
 * Copyright (C) 2018-2019 MindTouch, Inc.
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

namespace LambdaSharp.ConfigSource {

    public sealed class LambdaSystemEnvironmentSource : ILambdaConfigSource {

        //--- Fields ---
        private readonly string _prefix;

        //--- Constructors ---
        public LambdaSystemEnvironmentSource() {
            _prefix = "";
        }

        public LambdaSystemEnvironmentSource(string prefix) {
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        }

        //--- Methods ---
        public ILambdaConfigSource Open(string key) => new LambdaSystemEnvironmentSource(CombinePrefixWithKey(key));

        public string Read(string key) => Environment.GetEnvironmentVariable(CombinePrefixWithKey(key));

        public IEnumerable<string> ReadAllKeys() {
            var subpath = CombinePrefixWithKey("");
            return Environment.GetEnvironmentVariables().Keys.Cast<string>()
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

                // check if we need to remove a suffix as well (starting at '_')
                var indexOfUnderscore = subkey.IndexOf('_');
                if(indexOfUnderscore >= 0) {
                    subkey = subkey.Substring(0, indexOfUnderscore);
                }
                return subkey;
            }
        }
        private string CombinePrefixWithKey(string key) => ((_prefix.Length > 0) ? (_prefix + "_") : "") + key.ToUpperInvariant();
    }
}