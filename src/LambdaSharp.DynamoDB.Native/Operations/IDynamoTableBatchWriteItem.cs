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

using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Native.Operations {

    public interface IDynamoTableBatchWriteItems {

        //--- Methods ---
        IDynamoTableBatchWriteItemsPutItem<TRecord> BeginPutItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, TRecord record)
            where TRecord : class;
        IDynamoTableBatchWriteItems PutItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class;
        IDynamoTableBatchWriteItems DeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class;
        Task ExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default);
    }

    public interface IDynamoTableBatchWriteItemsPutItem<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableBatchWriteItemsPutItem<TRecord> Set(string key, AttributeValue value);
        IDynamoTableBatchWriteItems End();

        //--- Default Methods ---
        IDynamoTableBatchWriteItemsPutItem<TRecord> Set(string key, string value) => Set(key, new AttributeValue(value));
    }
}
