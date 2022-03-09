/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

namespace LambdaSharp.DynamoDB.Serialization {

    /// <summary>
    /// The <see cref="DynamoSerializationException"/> error is thrown when deserialization cannot proceed.
    /// </summary>
    public class DynamoSerializationException : Exception {

        //--- Constructors ---

        /// <summary>
        /// Creates a new instance of <see cref="DynamoSerializationException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public DynamoSerializationException(string message) : base(message) { }
    }
}
