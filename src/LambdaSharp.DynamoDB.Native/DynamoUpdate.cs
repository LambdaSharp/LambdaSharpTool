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

namespace LambdaSharp.DynamoDB.Native {

    /// <summary>
    /// The <see cref="DynamoUpdate"/> class contains static methods representing native DynamoDB
    /// update operators. These methods are only useful in <c>Set(...,...)</c>
    /// constructs to express DynamoDB conditions. When used in code directly, these methods
    /// throw <c>InvalidOperationException</c>.
    /// </summary>
    public static class DynamoUpdate {

        //--- Class Methods ---

        // *** `if_not_exists(path, value)` function ***

        /// <summary>
        /// The <see cref="IfNotExists{T}(T,T)"/> method represents the <c>if_not_exists(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="attribute">The record property to check.</param>
        /// <param name="value">The value to use if the record property does not exist.</param>
        /// <typeparam name="T">The type of the record property.</typeparam>
        public static T IfNotExists<T>(T attribute, T value) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="IfNotExists{T}(ISet{T},ISet{T})"/> method represents the <c>if_not_exists(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="attribute">The record property to check.</param>
        /// <param name="value">The value to use if the record property does not exist.</param>
        /// <typeparam name="T">The type of the record property.</typeparam>
        public static ISet<T> IfNotExists<T>(ISet<T> attribute, ISet<T> value) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="IfNotExists{T}(IDictionary{string,T},IDictionary{string,T})"/> method represents the <c>if_not_exists(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="attribute">The record property to check.</param>
        /// <param name="value">The value to use if the record property does not exist.</param>
        /// <typeparam name="T">The type of the record property.</typeparam>
        public static IDictionary<string, T> IfNotExists<T>(IDictionary<string, T> attribute, IDictionary<string, T> value) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="IfNotExists{T}(IList{T},IList{T})"/> method represents the <c>if_not_exists(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="attribute">The record property to check.</param>
        /// <param name="value">The value to use if the record property does not exist.</param>
        /// <typeparam name="T">The type of the record property.</typeparam>
        public static IList<T> IfNotExists<T>(IList<T> attribute, IList<T> value) => throw new InvalidOperationException();

        // *** `list_append(list1, list2)` function ***

        /// <summary>
        /// The <see cref="ListAppend{T}(IList{T},IList{T})"/> method represents the <c>list_append (list1, list2)</c> DynamoDB operation.
        /// </summary>
        /// <param name="list1">The list to append to.</param>
        /// <param name="list2">The list to append.</param>
        /// <typeparam name="T">The inner type for the generic list.</typeparam>
        public static IList<T> ListAppend<T>(IList<T> list1, IList<T> list2) => throw new InvalidOperationException();
    }
}
