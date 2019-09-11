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
using System.Collections.Generic;
using System.Linq;
using LambdaSharp.Exceptions;

namespace LambdaSharp.ConfigSource {

    /// <summary>
    /// The <see cref="LambdaDictionarySource"/> class is an implementation of
    /// <see cref="ILambdaConfigSource"/> interface that reads configuration values from
    /// a collection of key-value pairs. Nested sections are represented by a '/' character
    /// in the configuration key.
    /// </summary>
    public sealed class LambdaDictionarySource : ILambdaConfigSource {

        //--- Fields ---
        private readonly string _path;
        private readonly IDictionary<string, string> _parameters;

        //--- Constructors ---

        /// <summary>
        /// The <see cref="LambdaDictionarySource(IEnumerable{KeyValuePair{string,string}})"/> constructor creates
        /// a new <see cref="LambdaDictionarySource"/> instance from a collection of key-value pairs. Nested sections
        /// are represented by a '/' character in the configuration key.
        /// </summary>
        /// <param name="parameters">Collection of key-value pairs.</param>
        /// <returns>A <see cref="LambdaDictionarySource"/> instance.</returns>
        public LambdaDictionarySource(IEnumerable<KeyValuePair<string, string>> parameters) : this("", parameters) {}

        private LambdaDictionarySource(string path, IEnumerable<KeyValuePair<string, string>> parameters) {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _parameters = new Dictionary<string, string>(
                parameters ?? throw new ArgumentNullException(nameof(parameters)),
                StringComparer.OrdinalIgnoreCase
            );
        }

        //--- Methods ---

        /// <inheritdoc/>
        public ILambdaConfigSource Open(string name)
            => new LambdaDictionarySource(CombinePathWithKey(name), _parameters);

        /// <inheritdoc/>
        public string Read(string key) {

            // check if key contains invalid characters
            if(key.Any(c => !char.IsLetterOrDigit(c))) {
                throw new ArgumentException("argument must be alphanumeric", nameof(key));
            }
            var keyPath = CombinePathWithKey(key);
            if(_parameters.TryGetValue(keyPath, out var result)) {
                return result;
            }
            return null;
        }

        /// <inheritdoc/>
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

        private string CombinePathWithKey(string key) => _path + "/" +  key;
    }
}