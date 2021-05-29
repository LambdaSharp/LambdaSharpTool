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

using System.Collections.Generic;
using LambdaSharp.CloudFormation.Builder.Expressions;

namespace LambdaSharp.CloudFormation.Builder {

    public static class ACloudFormationBuilderItemEx {

        //--- Extension Methods ---

        // TODO: generalize to ASyntaxNode
        public static T Clone<T>(this T node) where T : ACloudFormationBuilderNode {
            var result = (T)node.CloneNode();
            result.SourceLocation = node.SourceLocation;
            return result;
        }

        public static IEnumerable<ACloudFormationBuilderNode> GetParents(this ACloudFormationBuilderNode node) {
            for(var parent = node.Parent; !(parent is null); parent = parent.Parent) {
                yield return parent;
            }
        }
    }
}