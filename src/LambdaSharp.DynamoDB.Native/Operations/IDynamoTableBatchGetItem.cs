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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LambdaSharp.DynamoDB.Native.Operations {

    public interface IDynamoTableBatchGetItems {

        //--- Methods ---
        IDynamoTableBatchGetItemsEntry<TRecord> BeginGetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class;
        Task<IEnumerable<object>> ExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default);

        //--- Default Methods ---
        IDynamoTableBatchGetItems GetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class
            => BeginGetItem(primaryKey, consistentRead).End();
    }

    public interface IDynamoTableBatchGetItemsEntry<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableBatchGetItemsEntry<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);
        IDynamoTableBatchGetItems End();
    }

    public interface IDynamoTableBatchGetItems<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableBatchGetItems<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);
        Task<IEnumerable<TRecord>> ExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default);
    }
}
