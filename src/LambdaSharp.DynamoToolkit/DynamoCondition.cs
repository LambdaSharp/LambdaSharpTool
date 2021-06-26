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

namespace LambdaSharp.DynamoToolkit {

    public static class DynamoCondition {

        // NOTE (2021-06-19, bjorg): see condition functions at https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.OperatorsAndFunctions.html

        //--- Class Methods ---
        public static bool Exists(object value) => throw new InvalidOperationException();
        public static bool DoesNotExist(object value) => throw new InvalidOperationException();
        public static bool HasType(object value, string type) => throw new InvalidOperationException();

        // *** `a BETWEEN b AND c` operator ***
        public static bool Between(string value, string lower, string upper) => throw new InvalidOperationException();
        public static bool Between(int value, int lower, int upper) => throw new InvalidOperationException();
        public static bool Between(long value, long lower, long upper) => throw new InvalidOperationException();
        public static bool Between(double value, double lower, double upper) => throw new InvalidOperationException();
        public static bool Between(decimal value, decimal lower, decimal upper) => throw new InvalidOperationException();

        // *** `a IN (b, c, d)` operator ***
        public static bool In(string value, IEnumerable<string> list) => throw new InvalidOperationException();
        public static bool In(int value, IEnumerable<int> list) => throw new InvalidOperationException();
        public static bool In(long value, IEnumerable<long> list) => throw new InvalidOperationException();
        public static bool In(double value, IEnumerable<double> list) => throw new InvalidOperationException();
        public static bool In(decimal value, IEnumerable<decimal> list) => throw new InvalidOperationException();

        // *** `begins_with(path, substr)` function ***
        public static bool BeginsWith(string value, string prefix) => throw new InvalidOperationException();

        // *** `contains(path, value)` function ***
        public static bool Contains(string value, string substr) => throw new InvalidOperationException();
        public static bool Contains(ISet<string> set, string item) => throw new InvalidOperationException();
        public static bool Contains(ISet<byte[]> set, byte[] item) => throw new InvalidOperationException();
        public static bool Contains(ISet<int> set, int item) => throw new InvalidOperationException();
        public static bool Contains(ISet<long> set, long item) => throw new InvalidOperationException();
        public static bool Contains(ISet<double> set, double item) => throw new InvalidOperationException();
        public static bool Contains(ISet<decimal> set, decimal item) => throw new InvalidOperationException();

        // *** `size(path)` function ***
        public static int Size(string value) => throw new InvalidOperationException();
        public static int Size(byte[] bytes) => throw new InvalidOperationException();
        public static int Size(ISet<byte[]> set) => throw new InvalidOperationException();
        public static int Size(ISet<string> set) => throw new InvalidOperationException();
        public static int Size(ISet<int> set) => throw new InvalidOperationException();
        public static int Size(ISet<long> set) => throw new InvalidOperationException();
        public static int Size(ISet<double> set) => throw new InvalidOperationException();
        public static int Size(ISet<decimal> set) => throw new InvalidOperationException();
        public static int Size<T>(IList<T> list) => throw new InvalidOperationException();
        public static int Size<T>(IDictionary<string, T> map) => throw new InvalidOperationException();
        public static int Size<T>(T map) where T : class => throw new InvalidOperationException();
    }
}
