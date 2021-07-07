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

    public interface IDynamoTableUpdateItem<TRecord> where TRecord : class {

        //--- Methods ---
        IDynamoTableUpdateItem<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition);

        // *** `SET Foo.Bar = :value` action ***
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, T>> attribute, T value);
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, ISet<T>>> attribute, ISet<T> value);
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IDictionary<string, T>>> attribute, IDictionary<string, T> value);
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IList<T>>> attribute, IList<T> value);
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, T>> attribute, Expression<Func<TRecord, T>> value);
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, ISet<T>>> attribute, Expression<Func<TRecord, ISet<T>>> value);
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IDictionary<string, T>>> attribute, Expression<Func<TRecord, IDictionary<string, T>>> value);
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IList<T>>> attribute, Expression<Func<TRecord, IList<T>>> value);
        IDynamoTableUpdateItem<TRecord> Set(string attribute, string value);

        // *** `REMOVE Brand` action ***
        IDynamoTableUpdateItem<TRecord> Remove<T>(Expression<Func<TRecord, T>> attribute);
        IDynamoTableUpdateItem<TRecord> Remove(string attribute);

        // *** `ADD Color :c` action ***
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, int>> attribute, int value);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, long>> attribute, long value);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, double>> attribute, double value);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, decimal>> attribute, decimal value);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<string>>> attribute, IEnumerable<string> values);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<byte[]>>> attribute, IEnumerable<byte[]> values);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<int>>> attribute, IEnumerable<int> values);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<long>>> attribute, IEnumerable<long> values);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<double>>> attribute, IEnumerable<double> values);
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<decimal>>> attribute, IEnumerable<decimal> values);

        // *** `DELETE Color :p` action ***
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<string>>> attribute, IEnumerable<string> values);
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<byte[]>>> attribute, IEnumerable<byte[]> values);
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<int>>> attribute, IEnumerable<int> values);
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<long>>> attribute, IEnumerable<long> values);
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<double>>> attribute, IEnumerable<double> values);
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<decimal>>> attribute, IEnumerable<decimal> values);

        // *** Execute UpdateItem ***
        Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
        Task<TRecord?> ExecuteReturnNewItemAsync(CancellationToken cancellationToken);
        Task<TRecord?> ExecuteReturnOldItemAsync(CancellationToken cancellationToken);
    }
}
