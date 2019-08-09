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

namespace LambdaSharp.ConfigSource {

    /// <summary>
    /// The <see cref="ILambdaConfigSource"/> interface provide the method
    /// definitions for accessing configuration values and nested sections.
    /// </summary>
    public interface ILambdaConfigSource {

        //--- Methods ---

        /// <summary>
        /// The <see cref="Open(string)"/> method returns an interface to read
        /// configuration values from the requested nested section. Section names
        /// are not case-sensitive.
        /// </summary>
        /// <param name="name">The name of the nested section.</param>
        /// <returns>The <see cref="ILambdaConfigSource"/> implementation of the nested section.</returns>
        ILambdaConfigSource Open(string name);

        /// <summary>
        /// The <see cref="Read(string)"/> method returns the configuration value
        /// of the specified key or <c>null</c> if the key does not exist. Configuration
        /// keys are not case-sensitive.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value or <c>null</c> if the key does not exist.</returns>
        string Read(string key);

        /// <summary>
        /// The <see cref="ReadAllKeys()"/> method returns all defined configuration keys.
        /// </summary>
        /// <returns>Enumeration of defined configuration keys.</returns>
        IEnumerable<string> ReadAllKeys();
    }
}
