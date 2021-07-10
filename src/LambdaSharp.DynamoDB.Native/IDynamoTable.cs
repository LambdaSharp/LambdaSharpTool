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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LambdaSharp.DynamoDB.Native.Operations;

namespace LambdaSharp.DynamoDB.Native {

    public interface IDynamoTable {

        // TODO (2021-07-05, bjorg): add 'Scan()` API
        // TODO (2021-07-05, bjorg): add 'TransactWriteItems()` API

        //--- Methods ---
        IDynamoTableGetItem<TRecord> GetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class;
        IDynamoTablePutItem<TRecord> PutItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, TRecord record)
            where TRecord : class;
        IDynamoTableUpdateItem<TRecord> UpdateItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class;
        IDynamoTableDeleteItem<TRecord> DeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class;
        IDynamoTableBatchGetItems<TRecord> BatchGetItems<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys, bool consistentRead = false)
            where TRecord : class;
        IDynamoTableBatchGetItems BatchGetItems(bool consistentRead = false);
        IDynamoTableBatchWriteItems BatchWriteItems();
        IDynamoTableTransactGetItems<TRecord> TransactGetItems<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys)
            where TRecord : class;
        IDynamoTableTransactGetItems TransactGetItems();
        IDynamoTableTransactWriteItems TransactWriteItems();
        IDynamoTableQuery Query(IDynamoQueryClause querySelect, int limit = int.MaxValue, bool scanIndexForward = true, bool consistentRead = false);
        IDynamoTableQuery<TRecord> Query<TRecord>(IDynamoQueryClause<TRecord> querySelect, int limit = int.MaxValue, bool scanIndexForward = true, bool consistentRead = false)
            where TRecord : class;

        //--- Default Methods ---
        Task<TRecord?> GetItemAsync<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false, CancellationToken cancellationToken = default)
            where TRecord : class
            => GetItem(primaryKey, consistentRead).ExecuteAsync(cancellationToken);

        Task<bool> PutItemAsync<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey, CancellationToken cancellationToken = default)
            where TRecord : class
            => PutItem(primaryKey, record).ExecuteAsync(cancellationToken);

        Task<bool> DeleteItemAsync<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, CancellationToken cancellationToken = default)
            where TRecord : class
            => DeleteItem(primaryKey).ExecuteAsync(cancellationToken);

        Task<TRecord?> DeleteAndReturnItemAsync<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, CancellationToken cancellationToken = default)
            where TRecord : class
            => DeleteItem(primaryKey).ExecuteReturnOldRecordAsync(cancellationToken);

        async Task<TRecord?> QuerySingleAsync<TRecord>(IDynamoQueryClause<TRecord> querySelect, bool consistentRead = false, CancellationToken cancellationToken = default)
            where TRecord : class
            => (await Query(querySelect, limit: 1, consistentRead: consistentRead).ExecuteFetchAllAttributesAsync(cancellationToken)).FirstOrDefault();    }
}
