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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LambdaSharp.DynamoDB.Serialization {

    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]> {

        //--- Class Fields ---
        public static readonly ByteArrayEqualityComparer Instance = new ByteArrayEqualityComparer();

        //--- Methods ---
        public bool Equals([AllowNull] byte[] left, [AllowNull] byte[] right) {
            if((left is null) && (right is null)) {
                return true;
            }
            if((left is null) || (right is null)) {
                return false;
            }
            return left.SequenceEqual(right);
        }

        public int GetHashCode([DisallowNull] byte[] obj) => obj.Length;
    }
}
