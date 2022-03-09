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
using System.Collections;
using System.Collections.Generic;

namespace LambdaSharp.DynamoDB.Native {

    /// <summary>
    /// The <see cref="DynamoCondition"/> class contains static methods representing native DynamoDB
    /// condition functions and operators. These methods are only useful in <c>WithCondition(...)</c>
    /// constructs to express DynamoDB conditions. When used in code directly, these methods
    /// throw <c>InvalidOperationException</c>.
    /// </summary>
    public static class DynamoCondition {

        // NOTE (2021-06-19, bjorg): see condition functions at https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html

        //--- Class Methods ---

        /// <summary>
        /// The <see cref="Exists"/> method represents the <c>attribute_exists(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record or record property to check for.</param>
        public static bool Exists(object value) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="DoesNotExist"/> method represents the <c>attribute_not_exists(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record or record property to check for.</param>
        public static bool DoesNotExist(object value) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="HasType"/> method represents the <c>attribute_type(path, type)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="type">The type to check.</param>
        public static bool HasType(object value, string type) => throw new InvalidOperationException();

        // *** `a BETWEEN b AND c` operator ***

        /// <summary>
        /// The <see cref="Between(string,string,string)"/> method represents the <c>a BETWEEN b AND c</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="lower">The lower bound (inclusive).</param>
        /// <param name="upper">The upper bound (inclusive).</param>
        public static bool Between(string value, string lower, string upper) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Between(int,int,int)"/> method represents the <c>a BETWEEN b AND c</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="lower">The lower bound (inclusive).</param>
        /// <param name="upper">The upper bound (inclusive).</param>
        public static bool Between(int value, int lower, int upper) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Between(long,long,long)"/> method represents the <c>a BETWEEN b AND c</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="lower">The lower bound (inclusive).</param>
        /// <param name="upper">The upper bound (inclusive).</param>
        public static bool Between(long value, long lower, long upper) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Between(double,double,double)"/> method represents the <c>a BETWEEN b AND c</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="lower">The lower bound (inclusive).</param>
        /// <param name="upper">The upper bound (inclusive).</param>
        public static bool Between(double value, double lower, double upper) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Between(decimal,decimal,decimal)"/> method represents the <c>a BETWEEN b AND c</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="lower">The lower bound (inclusive).</param>
        /// <param name="upper">The upper bound (inclusive).</param>
        public static bool Between(decimal value, decimal lower, decimal upper) => throw new InvalidOperationException();

        // *** `a IN (b, c, d)` operator ***

        /// <summary>
        /// The <see cref="In(string,IEnumerable{string})"/> method represents the <c>a IN (b, c, d)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="list">The non-empty list of values to check for.</param>
        public static bool In(string value, IEnumerable<string> list) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="In(int,IEnumerable{int})"/> method represents the <c>a IN (b, c, d)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="list">The non-empty list of values to check for.</param>
        public static bool In(int value, IEnumerable<int> list) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="In(long,IEnumerable{long})"/> method represents the <c>a IN (b, c, d)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="list">The non-empty list of values to check for.</param>
        public static bool In(long value, IEnumerable<long> list) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="In(double,IEnumerable{double})"/> method represents the <c>a IN (b, c, d)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="list">The non-empty list of values to check for.</param>
        public static bool In(double value, IEnumerable<double> list) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="In(decimal,IEnumerable{decimal})"/> method represents the <c>a IN (b, c, d)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="list">The non-empty list of values to check for.</param>
        public static bool In(decimal value, IEnumerable<decimal> list) => throw new InvalidOperationException();

        // *** `begins_with(path, substr)` function ***

        /// <summary>
        /// The <see cref="BeginsWith(string,string)"/> method represents the <c>begins_with(path, substr)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="prefix">The string prefix value to check for.</param>
        public static bool BeginsWith(string value, string prefix) => throw new InvalidOperationException();

        // *** `contains(path, value)` function ***

        /// <summary>
        /// The <see cref="Contains(string,string)"/> method represents the <c>contains(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to check.</param>
        /// <param name="substr">The sub-string to check for.</param>
        public static bool Contains(string value, string substr) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Contains(ISet{string},string)"/> method represents the <c>contains(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to check.</param>
        /// <param name="item">The item to check if it is present in the set.</param>
        public static bool Contains(ISet<string> set, string item) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Contains(ISet{byte[]},byte[])"/> method represents the <c>contains(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to check.</param>
        /// <param name="item">The item to check if it is present in the set.</param>
        public static bool Contains(ISet<byte[]> set, byte[] item) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Contains(ISet{int},int)"/> method represents the <c>contains(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to check.</param>
        /// <param name="item">The item to check if it is present in the set.</param>
        public static bool Contains(ISet<int> set, int item) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Contains(ISet{long},long)"/> method represents the <c>contains(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to check.</param>
        /// <param name="item">The item to check if it is present in the set.</param>
        public static bool Contains(ISet<long> set, long item) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Contains(ISet{double},double)"/> method represents the <c>contains(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to check.</param>
        /// <param name="item">The item to check if it is present in the set.</param>
        public static bool Contains(ISet<double> set, double item) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Contains(ISet{decimal},decimal)"/> method represents the <c>contains(path, value)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to check.</param>
        /// <param name="item">The item to check if it is present in the set.</param>
        public static bool Contains(ISet<decimal> set, decimal item) => throw new InvalidOperationException();

        // *** `size(path)` function ***

        /// <summary>
        /// The <see cref="Size(string)"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to get the character count of.</param>
        public static int Size(string value) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size(byte[])"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="value">The record property to get the byte size of.</param>
        public static int Size(byte[] value) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size(ISet{byte[]})"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to get the set item count of.</param>
        public static int Size(ISet<byte[]> set) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size(ISet{string})"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to get the set item count of.</param>
        public static int Size(ISet<string> set) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size(ISet{int})"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to get the set item count of.</param>
        public static int Size(ISet<int> set) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size(ISet{long})"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to get the set item count of.</param>
        public static int Size(ISet<long> set) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size(ISet{double})"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to get the set item count of.</param>
        public static int Size(ISet<double> set) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size(ISet{decimal})"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="set">The record property to get the set item count of.</param>
        public static int Size(ISet<decimal> set) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size(IList)"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="list">The record property to get the list item count of.</param>
        public static int Size(IList list) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size{T}(IList{T})"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="list">The record property to get the list item count of.</param>
        /// <typeparam name="T">The inner type for the generic list.</typeparam>
        public static int Size<T>(IList<T> list) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size{T}(IDictionary{string,T})"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="map">The record property to get the map item count of.</param>
        /// <typeparam name="T">The type of the record property.</typeparam>
        public static int Size<T>(IDictionary<string, T> map) => throw new InvalidOperationException();

        /// <summary>
        /// The <see cref="Size{T}(T)"/> method represents the <c>size(path)</c> DynamoDB operation.
        /// </summary>
        /// <param name="map">The record property to get the map item count of.</param>
        /// <typeparam name="T">The type of the record property.</typeparam>
        public static int Size<T>(T map) where T : class => throw new InvalidOperationException();
    }
}
