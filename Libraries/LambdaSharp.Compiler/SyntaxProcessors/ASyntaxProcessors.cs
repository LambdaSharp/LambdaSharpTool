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
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.Compiler.Syntax;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal abstract class ASyntaxProcessor {

        //--- Class Methods ---
        protected static string ToIdentifier(string text) => new string(text.Where(char.IsLetterOrDigit).ToArray());

        //--- Constructors ---
        public ASyntaxProcessor(ISyntaxProcessorDependencyProvider provider)
            => Provider = provider ?? throw new System.ArgumentNullException(nameof(provider));

        //--- Properties ---
        protected ISyntaxProcessorDependencyProvider Provider { get; }
        protected ILogger Logger => Provider.Logger;

        //--- Methods ---
        protected void InspectType<T>(Action<T> inspector)
            => Inspect(node => {
                if(node is T inspectableNode) {
                    inspector(inspectableNode);
                }
            });

        protected void Inspect(Action<ASyntaxNode> inspector) => Inspect(inspector, exitInspector: null);
        protected Task InspectAsync(Func<ASyntaxNode, Task> inspector) => InspectAsync(inspector, exitInspector: null);

        protected void Inspect(Action<ASyntaxNode>? entryInspector, Action<ASyntaxNode>? exitInspector) {
            foreach(var declaration in Provider.Declarations) {
                declaration.Inspect(entryInspector, exitInspector);
            }
        }

        protected async Task InspectAsync(Func<ASyntaxNode, Task>? entryInspector, Func<ASyntaxNode, Task>? exitInspector) {
            foreach(var declaration in Provider.Declarations) {
                await declaration.InspectAsync(entryInspector, exitInspector);
            }
        }

        protected void Substitute(Func<ISyntaxNode, ISyntaxNode> inspector) {
            foreach(var declaration in Provider.Declarations) {
                var result = declaration.Substitute(inspector);
                if(!object.ReferenceEquals(result, declaration)) {

                    // TODO: better exception
                    throw new ApplicationException("cannot substitute declarations");
                }
            }
        }
    }
}