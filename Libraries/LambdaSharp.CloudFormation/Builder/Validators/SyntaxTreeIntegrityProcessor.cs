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
using LambdaSharp.CloudFormation.Reporting;

namespace LambdaSharp.CloudFormation.Builder.Validators {

    /// <summary>
    /// The <see cref="SyntaxTreeIntegrityProcessor"/> class checks the basic data-structure integrity of the syntax tree.
    /// </summary>
    internal sealed class SyntaxTreeIntegrityProcessor : ASyntaxProcessor {

        //--- Types ---
        private class SyntaxTreeIntegrityException : Exception {

            //--- Constructors ---
            public SyntaxTreeIntegrityException(string message) : base(message) { }
        }

        //--- Constructors ---
        public SyntaxTreeIntegrityProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void ValidateIntegrity(CloudFormationBuilderTemplate template) {
            var found = new HashSet<object>();
            template.Inspect(node => {

                // verify AST has no cycles
                if(!found.Add(node)) {
                    throw new SyntaxTreeIntegrityException("found cycle");
                }

                // every node must have a parent, except the starting module
                if(!object.ReferenceEquals(node, template) && (node.Parent == null)) {
                    throw new SyntaxTreeIntegrityException("missing parent");
                }

                // every node must have a source location
                if(node.SourceLocation == null) {
                    throw new SyntaxTreeIntegrityException("missing source location");
                }
            }, node => found.Remove(node));
        }
    }
}