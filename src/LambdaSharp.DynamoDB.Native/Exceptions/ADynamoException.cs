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
using System.Runtime.Serialization;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Native.Exceptions {

    public abstract class ADynamoException : Exception {

        //--- Constructors ---
        protected ADynamoException(string? message) : base(message) { }
        protected ADynamoException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        protected ADynamoException(string? message, Exception? innerException) : base(message, innerException) { }
    }

    public class DynamoTableBatchGetItemMaxAttemptsExceededException : ADynamoException {

        //--- Constructors ---
        internal DynamoTableBatchGetItemMaxAttemptsExceededException(IEnumerable<object> items)
            : base("max attempts exceeded")
            => Items = items ?? throw new ArgumentNullException(nameof(items));

        //--- Properties ---
        public IEnumerable<object> Items { get; }
    }

    public class DynamoTableBatchWriteItemMaxAttemptsExceededException : ADynamoException {

        //--- Constructors ---
        internal DynamoTableBatchWriteItemMaxAttemptsExceededException(IEnumerable<WriteRequest> unprocessedItems)
            : base("max attempts exceeded")
            => UnprocessedItems = unprocessedItems ?? throw new ArgumentNullException(nameof(unprocessedItems));

        //--- Properties ---
        public IEnumerable<WriteRequest> UnprocessedItems { get; }
    }
}
