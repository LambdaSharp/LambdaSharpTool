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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace LambdaSharp.SimpleQueueService {

    /// <summary>
    /// The <see cref="ILambdaQueueFunctionDependencyProvider"/> interface provides all the required dependencies
    /// for <see cref="ALambdaQueueFunction{TMessage}"/> instances. This interface follows the <i>Dependency Provider</i> pattern
    /// where all side-effecting methods and properties must be provided by an outside implementation.
    /// </summary>
    public interface ILambdaQueueFunctionDependencyProvider : ILambdaFunctionDependencyProvider {

        //--- Methods ---

        /// <summary>
        /// Delete a batch of message from an SQS queue. The Lambda function requires <c>sqs:DeleteMessageBatch</c> permission
        /// on the specified SQS URL.
        /// </summary>
        /// <param name="queueUrl">SQS URL.</param>
        /// <param name="messages">Enumeration of <c>(string MessageId, string ReceiptHandle)</c> tuples specifying which messages to delete.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task DeleteMessagesFromQueueAsync(string queueUrl, IEnumerable<(string MessageId, string ReceiptHandle)> messages);
    }
}