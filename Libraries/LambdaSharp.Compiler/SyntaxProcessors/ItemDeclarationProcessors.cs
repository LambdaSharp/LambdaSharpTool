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
using LambdaSharp.CloudFormation;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.SyntaxProcessors {
    using ErrorFunc = Func<string, Error>;

    /// <summary>
    /// The <see cref="ItemDeclarationProcessor"/> class validates that all declaration names are valid and unique.
    /// </summary>
    internal sealed class ItemDeclarationProcessor : ASyntaxProcessor {

        //--- Class Fields ---
        private static readonly ErrorFunc ReservedName = parameter => new Error($"'{parameter}' is a reserved name");
        private static readonly Error NameMustBeAlphanumeric = new Error("name must be alphanumeric");
        private static readonly ErrorFunc DuplicateName = parameter => new Error($"duplicate name '{parameter}'");
        private static readonly ErrorFunc AmbiguousLogicalId = parameter => new Error($"ambiguous logical ID for '{parameter}'");

        //--- Constructors ---
        public ItemDeclarationProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Declare(ModuleDeclaration moduleDeclaration) {
            var logicalIds = new HashSet<string>();
            moduleDeclaration.Inspect(node => {
                switch(node) {
                case ResourceTypeDeclaration _:

                    // resource types cannot be referenced with !Ref/!GetAtt
                    break;
                case AItemDeclaration itemDeclaration:
                    ValidateItemDeclaration(itemDeclaration);
                    break;
                }
            });

            // local functions
            void ValidateItemDeclaration(AItemDeclaration declaration) {
                if(!CloudFormationValidationRules.IsValidCloudFormationName(declaration.ItemName.Value)) {

                    // declaration name is not valid
                    Logger.Log(NameMustBeAlphanumeric, declaration);
                } else if(
                    CloudFormationValidationRules.IsReservedCloudFormationName(declaration.FullName)
                    && !(declaration is PseudoParameterDeclaration)
                ) {

                    // declaration uses a reserved name
                    Logger.Log(ReservedName(declaration.FullName), declaration);
                } else if(Provider.TryGetItem(declaration.FullName, out _)) {

                    // full name is not unique
                    Logger.Log(DuplicateName(declaration.FullName), declaration);
                } else if(!logicalIds.Add(declaration.LogicalId)) {

                    // logical ID is ambiguous
                    Logger.Log(AmbiguousLogicalId(declaration.FullName));
                } else {

                    // add declaration
                    Provider.DeclareItem(declaration.Parent, declaration);
                }
            }
        }
    }
}