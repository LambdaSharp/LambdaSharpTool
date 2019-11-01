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

using System.Collections.Generic;

namespace LambdaSharp.Tool.Parser.Syntax {

    public abstract class ASyntaxNode {

        //--- Properties ---
        public ASyntaxNode Parent { get; set; }
        public SourceLocation SourceLocation { get; set; }

        public IEnumerable<ASyntaxNode> Parents {
            get {
                var node = this;
                while(node.Parent != null) {
                    yield return node.Parent;
                    node = node.Parent;
                }
            }
        }

        //--- Abstract Methods ---
        public abstract void Visit(ASyntaxNode parent, ISyntaxVisitor visitor);
    }

    public static class ASyntaxNodeEx {

        //--- Extension Methods ---
        public static void Visit(this IEnumerable<ASyntaxNode> nodes, ASyntaxNode parent, ISyntaxVisitor visitor) {
            foreach(var node in nodes) {
                node?.Visit(parent, visitor);
            }
        }
    }
}