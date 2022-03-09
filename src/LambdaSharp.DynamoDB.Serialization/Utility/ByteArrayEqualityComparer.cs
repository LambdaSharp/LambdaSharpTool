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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LambdaSharp.DynamoDB.Serialization.Utility {

    /// <summary>
    /// The <see cref="ByteArrayEqualityComparer"/> class implements equality comparision for byte arrays.
    /// </summary>
    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]> {

        //--- Class Fields ---

        /// <summary>
        /// The <see cref="Instance"/> class field exposes a reusable instance of the <see cref="ByteArrayEqualityComparer"/> class.
        /// </summary>
        public static readonly ByteArrayEqualityComparer Instance = new ByteArrayEqualityComparer();

        //--- Methods ---

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="left">The first object of type T to compare.</param>
        /// <param name="right">The second object of type T to compare.</param>
        /// <returns><c>true</c> if the specified objects are equal; otherwise, <c>false</c>.</returns>
        public bool Equals([AllowNull] byte[] left, [AllowNull] byte[] right) {
            if((left is null) && (right is null)) {
                return true;
            }
            if((left is null) || (right is null)) {
                return false;
            }
            return left.SequenceEqual(right);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <param name="obj">The System.Object for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified object.</returns>
        public int GetHashCode([DisallowNull] byte[] obj) => obj.Length;
    }
}
