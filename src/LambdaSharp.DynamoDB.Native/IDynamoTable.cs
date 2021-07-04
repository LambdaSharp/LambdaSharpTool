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
using LambdaSharp.DynamoDB.Native.Operations;

namespace LambdaSharp.DynamoDB.Native {

    public interface IDynamoTable {

        //--- Methods ---
        IDynamoTableGetItem<TRecord> GetItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, bool consistentRead = false)
            where TRecord : class;
        IDynamoTablePutItem<TRecord> PutItem<TRecord>(TRecord record, DynamoPrimaryKey<TRecord> primaryKey, params ADynamoSecondaryKey[] secondaryKeys)
            where TRecord : class;
        IDynamoTableUpdateItem<TRecord> UpdateItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class;
        IDynamoTableDeleteItem<TRecord> DeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class;
        IDynamoTableQuery<TRecord> Query<TRecord>(IDynamoQuerySelect<TRecord> querySelect, int limit = int.MaxValue, bool scanIndexForward = true, bool consistentRead = false)
            where TRecord : class;
        IDynamoTableBatchGetItems<TRecord> BatchGetItems<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys, bool consistentRead = false)
            where TRecord : class;
        IDynamoTableBatchGetItems BatchGetItemsMixed(bool consistentRead = false);
        IDynamoTableBatchWriteItems BatchWriteItems();
        IDynamoTableTransactGetItems<TRecord> TransactGetItems<TRecord>(IEnumerable<DynamoPrimaryKey<TRecord>> primaryKeys)
            where TRecord : class;
        IDynamoTableTransactGetItems TransactGetItemsMixed();
    }
}
