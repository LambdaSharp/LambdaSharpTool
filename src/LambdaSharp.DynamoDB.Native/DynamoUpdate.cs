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

namespace LambdaSharp.DynamoDB.Native {

    public static class DynamoUpdate {

        //--- Class Methods ---

        // *** `if_not_exists(path, value)` function ***
        public static T IfNotExists<T>(T attribute, T value) => throw new InvalidOperationException();
        public static ISet<T> IfNotExists<T>(ISet<T> attribute, ISet<T> value) => throw new InvalidOperationException();
        public static IDictionary<string, T> IfNotExists<T>(IDictionary<string, T> attribute, IDictionary<string, T> value) => throw new InvalidOperationException();
        public static IList<T> IfNotExists<T>(IList<T> attribute, IList<T> value) => throw new InvalidOperationException();

        // *** `list_append (list1, list2)` function ***
        public static IList<T> ListAppend<T>(IList<T> attribute, IList<T> values) => throw new InvalidOperationException();
    }
}
