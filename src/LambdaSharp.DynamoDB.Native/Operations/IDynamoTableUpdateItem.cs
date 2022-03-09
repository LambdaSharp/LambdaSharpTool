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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace LambdaSharp.DynamoDB.Native.Operations {

    /// <summary>
    /// Interface to specify a UpdateItem operation.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    public interface IDynamoTableUpdateItem<TRecord> where TRecord : class {

        //--- Methods ---

        /// <summary>
        /// Add condition for UpdateItem operation.
        /// </summary>
        /// <param name="condition">A lambda predicate representing the DynamoDB condition expression.</param>
        IDynamoTableUpdateItem<TRecord> WithCondition(Expression<Func<TRecord, bool>> condition);

        // *** `SET Foo.Bar = :value` action ***

        /// <summary>
        /// Set a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, T>> attribute, T value);

        /// <summary>
        /// Set a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, ISet<T>>> attribute, ISet<T> value);

        /// <summary>
        /// Set a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IDictionary<string, T>>> attribute, IDictionary<string, T> value);

        /// <summary>
        /// Set a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IList<T>>> attribute, IList<T> value);

        /// <summary>
        /// Set a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, T>> attribute, Expression<Func<TRecord, T>> value);

        /// <summary>
        /// Set a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, ISet<T>>> attribute, Expression<Func<TRecord, ISet<T>>> value);

        /// <summary>
        /// Set a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IDictionary<string, T>>> attribute, Expression<Func<TRecord, IDictionary<string, T>>> value);

        /// <summary>
        /// Set a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Set<T>(Expression<Func<TRecord, IList<T>>> attribute, Expression<Func<TRecord, IList<T>>> value);

        /// <summary>
        /// Set the value of a DynamoDB item attribute. Used for storing attributes used by local/global secondary indices.
        /// </summary>
        /// <param name="key">Name of attribute.</param>
        /// <param name="value">Value of attribute.</param>
        IDynamoTableUpdateItem<TRecord> Set(string key, AttributeValue value);

        // *** `REMOVE Brand` action ***

        /// <summary>
        /// Remove a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <typeparam name="T">The property type.</typeparam>
        IDynamoTableUpdateItem<TRecord> Remove<T>(Expression<Func<TRecord, T>> attribute);

        /// <summary>
        /// Remove a DynamoDB item attribute. Used for removing attributes used by local/global secondary indices.
        /// </summary>
        /// <param name="key">Name of attribute.</param>
        IDynamoTableUpdateItem<TRecord> Remove(string key);

        // *** `ADD Color :c` action ***

        /// <summary>
        /// Add a value to a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">Value to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, int>> attribute, int value);

        /// <summary>
        /// Add a value to a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">Value to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, long>> attribute, long value);

        /// <summary>
        /// Add a value to a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">Value to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, double>> attribute, double value);

        /// <summary>
        /// Add a value to a record property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record property.</param>
        /// <param name="value">Value to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, decimal>> attribute, decimal value);

        /// <summary>
        /// Add one or more values to a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<string>>> attribute, IEnumerable<string> values);

        /// <summary>
        /// Add one or more values to a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<byte[]>>> attribute, IEnumerable<byte[]> values);

        /// <summary>
        /// Add one or more values to a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<int>>> attribute, IEnumerable<int> values);

        /// <summary>
        /// Add one or more values to a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<long>>> attribute, IEnumerable<long> values);

        /// <summary>
        /// Add one or more values to a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<double>>> attribute, IEnumerable<double> values);

        /// <summary>
        /// Add one or more values to a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to add.</param>
        IDynamoTableUpdateItem<TRecord> Add(Expression<Func<TRecord, ISet<decimal>>> attribute, IEnumerable<decimal> values);

        // *** `DELETE Color :p` action ***

        /// <summary>
        /// Delete one or more values from a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to delete.</param>
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<string>>> attribute, IEnumerable<string> values);

        /// <summary>
        /// Delete one or more values from a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to delete.</param>
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<byte[]>>> attribute, IEnumerable<byte[]> values);

        /// <summary>
        /// Delete one or more values from a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to delete.</param>
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<int>>> attribute, IEnumerable<int> values);

        /// <summary>
        /// Delete one or more values from a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to delete.</param>
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<long>>> attribute, IEnumerable<long> values);

        /// <summary>
        /// Delete one or more values from a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to delete.</param>
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<double>>> attribute, IEnumerable<double> values);

        /// <summary>
        /// Delete one or more values from a record set property.
        /// </summary>
        /// <param name="attribute">A lambda expression that selects the target record set property.</param>
        /// <param name="values">Values to delete.</param>
        IDynamoTableUpdateItem<TRecord> Delete(Expression<Func<TRecord, ISet<decimal>>> attribute, IEnumerable<decimal> values);

        // *** Execute UpdateItem ***

        /// <summary>
        /// Execute the UpdateItem operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>True, when successful. False, when condition is not met.</returns>
        Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute the UpdateItem operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Updated record when found and condition is met. <c>null</c>, otherwise.</returns>
        Task<TRecord?> ExecuteReturnNewItemAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Execute the UpdateItem operation.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Old record when found and condition is met. <c>null</c>, otherwise.</returns>
        Task<TRecord?> ExecuteReturnOldItemAsync(CancellationToken cancellationToken);

        //--- Default Methods ---

        /// <summary>
        /// Set the value of a DynamoDB item attribute. Used for storing attributes used by local/global secondary indices.
        /// </summary>
        /// <param name="key">Name of attribute.</param>
        /// <param name="value">Value of attribute.</param>
        IDynamoTableUpdateItem<TRecord> Set(string key, string value)
            => Set(key, new AttributeValue(value));
    }
}
