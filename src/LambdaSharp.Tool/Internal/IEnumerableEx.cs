/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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

namespace LambdaSharp.Tool.Internal {

    public static class IEnumerableEx {

        //--- Extension Methods ---
        public static IEnumerable<T> Distinct<T,V>(this IEnumerable<T> items, Func<T,V> discriminator) {
            var seen = new HashSet<V>();
            foreach(var item in items) {
                var key = discriminator(item);
                if(seen.Add(key)) {
                    yield return item;
                }
            }
        }
    }
}