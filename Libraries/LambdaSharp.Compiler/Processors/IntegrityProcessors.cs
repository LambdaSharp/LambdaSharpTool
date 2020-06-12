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

using System;
using System.Collections.Generic;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.Processors {

    internal sealed class IntegrityProcessor : AProcessor {

        //--- Constructors ---
        public IntegrityProcessor(IProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration) {
            var found = new HashSet<object>();
            moduleDeclaration.Inspect(node => {

                // verify AST has no cycles
                if(!found.Add(node)) {

                    // TODO: better exception
                    throw new Exception("found cycle");
                }

                // every node must have a parent, exception the starting module
                if(!object.ReferenceEquals(node, moduleDeclaration) && (node.Parent == null)) {

                    // TODO: better exception
                    throw new Exception("missing parent");
                }

                // every node must have a source location
                if(node.SourceLocation == null) {

                    // TODO: better exception
                    throw new Exception("missing source location");
                }
            }, node => found.Remove(node));
        }
    }
}