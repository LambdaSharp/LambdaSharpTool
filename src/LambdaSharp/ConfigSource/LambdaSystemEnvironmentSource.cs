/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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

namespace LambdaSharp.ConfigSource {

    /// <summary>
    /// The <see cref="LambdaDictionarySource"/> class is an implementation of
    /// <see cref="ILambdaConfigSource"/> interface that reads configuration values from
    /// system environment variables. Nested sections are represented by a '_' character
    /// in the system environment variable name.
    /// </summary>
    public sealed class LambdaSystemEnvironmentSource : ILambdaConfigSource {

        //--- Fields ---
        private readonly string _prefix;

        //--- Constructors ---

        /// <summary>
        /// The <see cref="LambdaDictionarySource(IEnumerable{KeyValuePair{string,string}})"/> constructor creates
        /// a new <see cref="LambdaDictionarySource"/> instance from a collection of key-value pairs. Nested sections
        /// are represented by a '_' character in the system environment variable name.
        /// </summary>
        public LambdaSystemEnvironmentSource() : this("") { }

        private LambdaSystemEnvironmentSource(string prefix) {
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        }

        //--- Methods ---

        /// <summary>
        /// The <see cref="Open(string)"/> method returns an interface to read
        /// configuration values from the requested nested section. Section names
        /// are not case-sensitive.
        /// </summary>
        /// <param name="name">The name of the nested section.</param>
        /// <returns>The <see cref="ILambdaConfigSource"/> implementation of the nested section.</returns>
        public ILambdaConfigSource Open(string name) => new LambdaSystemEnvironmentSource(CombinePrefixWithKey(name));

        /// <summary>
        /// The <see cref="Read(string)"/> method returns the configuration value
        /// of the specified key or <c>null</c> if the key does not exist. Configuration
        /// keys are not case-sensitive.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value or <c>null</c> if the key does not exist.</returns>
        public string Read(string key) => Environment.GetEnvironmentVariable(CombinePrefixWithKey(key));

        /// <summary>
        /// The <see cref="ReadAllKeys()"/> method returns all defined configuration keys.
        /// </summary>
        /// <returns>Enumeration of defined configuration keys.</returns>
        public IEnumerable<string> ReadAllKeys() {
            var subpath = CombinePrefixWithKey("");
            return Environment.GetEnvironmentVariables()
                .Keys
                .Cast<string>()
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