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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LambdaSharp.DynamoDB.Native {

    public static class IDynamoTableEx {

        //--- Extension Methods ---
        public static Task<TRecord?> GetItemAsync<TRecord>(this IDynamoTable table, DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false, CancellationToken cancellationToken = default)
            where TRecord : class
            => table.GetItem(primaryKey, consistentRead).ExecuteAsync(cancellationToken);

        public static async Task<TRecord?> QuerySingleAsync<TRecord>(this IDynamoTable table, IDynamoQuerySelect<TRecord> querySelect, bool consistentRead = false, CancellationToken cancellationToken = default)
            where TRecord : class
            => (await table.Query(querySelect, limit: 1, consistentRead: consistentRead).ExecuteFetchAllAttributesAsync(cancellationToken)).FirstOrDefault();

        public static Task<bool> PutItemAsync<TRecord>(this IDynamoTable table, TRecord record, DynamoPrimaryKey<TRecord> primaryKey, CancellationToken cancellationToken = default)
            where TRecord : class
            => table.PutItem(record, primaryKey).ExecuteAsync(cancellationToken);

        public static Task<bool> DeleteItemAsync<TRecord>(this IDynamoTable table, DynamoPrimaryKey<TRecord> primaryKey, CancellationToken cancellationToken = default)
            where TRecord : class
            => table.DeleteItem(primaryKey).ExecuteAsync(cancellationToken);

        public static Task<TRecord?> DeleteAndReturnItemAsync<TRecord>(this IDynamoTable table, DynamoPrimaryKey<TRecord> primaryKey, CancellationToken cancellationToken = default)
            where TRecord : class
            => table.DeleteItem(primaryKey).ExecuteReturnOldItemAsync(cancellationToken);
    }
}
