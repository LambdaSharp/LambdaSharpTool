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

namespace LambdaSharp.Logging {

    /// <summary>
    /// The abstract <see cref="ALambdaLogRecord"/> class is the base class for all
    /// structured log records.
    /// </summary>
    public abstract class ALambdaLogRecord {

        //--- Properties ---

        /// <summary>
        /// The <see cref="Type"/> property determines the type of the Lambda record.
        /// </summary>
        /// <value>The source of the Lambda record.</value>
        public string Type { get; set; }

        /// <summary>
        /// The <see cref="Version"/> property determines the format version of the Lambda record.
        /// </summary>
        /// <value>The format version.</value>
        public string Version { get; set; }
    }
}
