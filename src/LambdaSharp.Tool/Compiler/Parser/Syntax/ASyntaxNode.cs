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

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class ASyntaxNode {

        //--- Fields ---
        private SourceLocation? _sourceLocation;

        //--- Properties ---
        public ASyntaxNode? Parent { get; private set; }

        public SourceLocation? SourceLocation {

            // TODO: consider return a default empty location when no source location is found
            get => _sourceLocation ?? Parent?.SourceLocation;
            set => _sourceLocation = value;
        }

        public IEnumerable<ASyntaxNode> Parents {
            get {
                var node = this;
                while(node.Parent != null) {
                    yield return node.Parent;
                    node = node.Parent;
                }
            }
        }

        public AItemDeclaration ParentItemDeclaration => Parents.OfType<AItemDeclaration>().First();
        public ModuleDeclaration ParentModuleDeclaration => Parents.OfType<ModuleDeclaration>().First();

        //--- Abstract Methods ---
        public abstract void Visit(ASyntaxNode parent, ISyntaxVisitor visitor);

        //--- Methods ---
        [return: NotNullIfNotNull("node") ]
        protected T? SetParent<T>(T? node) where T : ASyntaxNode {
            if(node != null) {

                // TODO: should we enforce this?
                // if(node.Parent != null) {
                //     throw new ApplicationException("node already had a parent");
                // }
                node.Parent = this;
            }
            return node;
        }

        [return: NotNullIfNotNull("list") ]
        protected List<T>? SetParent<T>(List<T>? list) where T : ASyntaxNode {
            if(list != null) {
                foreach(var node in list) {
                    SetParent(node);
                }
            }
            return list;
        }
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