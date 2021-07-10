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

    public interface IDynamoTableTransactWriteItems {

        //--- Methods ---
        IDynamoTableTransactWriteItemsPutItem<TRecord> BeginPutItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, TRecord record) where TRecord : class;
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> BeginUpdateItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey) where TRecord : class;
        IDynamoTableTransactWriteItemsDeleteItem<TRecord> BeginDeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey) where TRecord : class;
        IDynamoTableTransactWriteItemsConditionCheck<TRecord> BeginConditionCheck<TRecord>(DynamoPrimaryKey<TRecord> primaryKey) where TRecord : class;
        Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);

        //--- Default Methods ---
        IDynamoTableTransactWriteItems PutItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey, TRecord record)
            where TRecord : class
            => BeginPutItem(primaryKey, record).End();

        IDynamoTableTransactWriteItems DeleteItem<TRecord>(DynamoPrimaryKey<TRecord> primaryKey)
            where TRecord : class
            => BeginDeleteItem(primaryKey).End();
    }

    public interface IDynamoTableTransactWriteItemsPutItem<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableTransactWriteItemsPutItem<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition);
        IDynamoTableTransactWriteItems End();
    }

    public interface IDynamoTableTransactWriteItemsUpdateItem<TRecord> where TRecord : class  {

        //--- Methods ---
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition);
        IDynamoTableTransactWriteItems End();

        // *** `SET Foo.Bar = :value` action ***
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, T>> attribute, T value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, ISet<T>>> attribute, ISet<T> value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IDictionary<string, T>>> attribute, IDictionary<string, T> value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IList<T>>> attribute, IList<T> value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, T>> attribute, Expression<Func<TRecord, T>> value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, ISet<T>>> attribute, Expression<Func<TRecord, ISet<T>>> value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IDictionary<string, T>>> attribute, Expression<Func<TRecord, IDictionary<string, T>>> value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IList<T>>> attribute, Expression<Func<TRecord, IList<T>>> value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Set(string attribute, string value);

        // *** `REMOVE Brand` action ***
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Remove<T>(Expression<Func<TRecord, T>> attribute);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Remove(string attribute);

        // *** `ADD Color :c` action ***
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, int>> attribute, int value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, long>> attribute, long value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, double>> attribute, double value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, decimal>> attribute, decimal value);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<string>>> attribute, IEnumerable<string> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<byte[]>>> attribute, IEnumerable<byte[]> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<int>>> attribute, IEnumerable<int> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<long>>> attribute, IEnumerable<long> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<double>>> attribute, IEnumerable<double> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<decimal>>> attribute, IEnumerable<decimal> values);

        // *** `DELETE Color :p` action ***
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<string>>> attribute, IEnumerable<string> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<byte[]>>> attribute, IEnumerable<byte[]> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<int>>> attribute, IEnumerable<int> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<long>>> attribute, IEnumerable<long> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<double>>> attribute, IEnumerable<double> values);
        IDynamoTableTransactWriteItemsUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<decimal>>> attribute, IEnumerable<decimal> values);
    }

    public interface IDynamoTableTransactWriteItemsDeleteItem<TRecord> where TRecord : class  {

        //--- Methods ---
        IDynamoTableTransactWriteItemsDeleteItem<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition);
        IDynamoTableTransactWriteItems End();
    }

    public interface IDynamoTableTransactWriteItemsConditionCheck<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableTransactWriteItemsConditionCheck<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition);
        IDynamoTableTransactWriteItems End();
    }
}
