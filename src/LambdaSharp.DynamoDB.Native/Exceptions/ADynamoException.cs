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
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Native.Exceptions {

    /// <summary>
    /// The <see cref="ADynamoException"/> is the base class for all exceptions thrown the LambdaSharp.DynamoDB.Native library.
    /// </summary>
    public abstract class ADynamoException : Exception {

        //--- Constructors ---

        /// <summary>
        /// Initialize new instance of <see cref="ADynamoException"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected ADynamoException(string? message) : base(message) { }

        /// <summary>
        /// Initialize new instance of <see cref="ADynamoException"/>.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference.</param>
        protected ADynamoException(string? message, Exception? innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// The <see cref="DynamoTableBatchGetItemsMaxAttemptsExceededException"/> exception is throw when
    /// <see cref="IDynamoTable.BatchGetItems{TRecord}(IEnumerable{DynamoPrimaryKey{TRecord}}, bool)"/>
    /// or <see cref="IDynamoTable.BatchGetItems(bool)"/> have reached they retry limit.
    /// </summary>
    public class DynamoTableBatchGetItemsMaxAttemptsExceededException : ADynamoException {

        //--- Constructors ---
        internal DynamoTableBatchGetItemsMaxAttemptsExceededException(IEnumerable<object> items)
            : base("max BatchGetItems attempts exceeded")
            => Items = items ?? throw new ArgumentNullException(nameof(items));

        //--- Properties ---

        /// <summary>
        /// The items that were successfully retrieved by the BatchGetItems operation.
        /// </summary>
        public IEnumerable<object> Items { get; }
    }

    /// <summary>
    /// The <see cref="DynamoTableBatchWriteItemsMaxAttemptsExceededException"/> exception is throw when
    /// <see cref="IDynamoTable.BatchWriteItems()"/> has reached its retry limit.
    /// </summary>
    public class DynamoTableBatchWriteItemsMaxAttemptsExceededException : ADynamoException {

        //--- Constructors ---
        internal DynamoTableBatchWriteItemsMaxAttemptsExceededException(IEnumerable<WriteRequest> unprocessedItems)
            : base("max BatchWriteItems attempts exceeded")
            => UnprocessedItems = unprocessedItems ?? throw new ArgumentNullException(nameof(unprocessedItems));

        //--- Properties ---

        /// <summary>
        /// The items that were not successfully written by the BatchWriteItems operation.
        /// </summary>
        public IEnumerable<WriteRequest> UnprocessedItems { get; }
    }
}
