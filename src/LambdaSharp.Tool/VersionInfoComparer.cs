/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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


using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LambdaSharp.Tool {

    public class VersionInfoComparer : IComparer<VersionInfo> {

        //--- Methods ---
        public int Compare([AllowNull] VersionInfo left, [AllowNull] VersionInfo right) {
            if((left == null) && (right == null)) {
                return 0;
            }
            if(left == null) {
                return -1;
            }
            if(right == null) {
                return 1;
            }
            var result = left.CompareToVersion(right);
            if(result.HasValue) {
                return result.Value;
            }
            return left.Suffix.ToLowerInvariant().CompareTo(right.Suffix.ToLowerInvariant());
        }
    }
}