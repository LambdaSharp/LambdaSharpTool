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

namespace LambdaSharp.Compiler.Validators {
    using ErrorFunc = Func<string, Error>;

    internal sealed class ItemDeclarationValidator : AValidator {

        //--- Class Fields ---
        private static readonly ErrorFunc ReservedName = parameter => new Error(0, $"'{parameter}' is a reserved name");

        //--- Constructors ---
        public ItemDeclarationValidator(IValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration) {
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
                    Logger.Log(Error.NameMustBeAlphanumeric, declaration);
                } else if(
                    !declaration.AllowReservedName
                    && CloudFormationValidationRules.IsReservedCloudFormationName(declaration.FullName)
                ) {

                    // declaration uses a reserved name
                    Logger.Log(ReservedName(declaration.FullName), declaration);
                } else if(Provider.TryGetItem(declaration.FullName, out var _)) {

                    // full name is not unique
                    Logger.Log(Error.DuplicateName(declaration.FullName), declaration);
                } else if(!logicalIds!.Add(declaration.LogicalId)) {

                    // logical ID is ambiguous
                    Logger.Log(Error.AmbiguousLogicalId(declaration.FullName));
                } else {
                    Provider.DeclareItem(declaration);
                }
            }
        }
    }
}