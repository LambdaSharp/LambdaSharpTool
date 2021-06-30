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


    public interface IDynamoTableBatchGetItem<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableBatchGetItem<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);
        Task<IEnumerable<TRecord>> ExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default);
    }

    public interface IDynamoTableBatchGetItem {

        //--- Methods ---
        IDynamoTableBatchGetItemEntry<TRecord> AddGetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class;
        Task<IEnumerable<object>> ExecuteAsync(int maxAttempts = 5, CancellationToken cancellationToken = default);
    }

    public interface IDynamoTableBatchGetItemEntry<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableBatchGetItemEntry<TRecord> Get<T>(Expression<Func<TRecord, T>> attribute);
        IDynamoTableBatchGetItem Execute(CancellationToken cancellationToken = default);
    }
}
