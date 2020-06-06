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

using System;
using System.Collections.Generic;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.Validators {
    using ErrorFunc = Func<string, Error>;

    internal sealed class ItemDeclarationValidator : AValidator {

        //--- Class Fields ---
        private static readonly ErrorFunc ReservedName = parameter => new Error(0, $"'{parameter}' is a reserved name");

        //--- Constructors ---
        public ItemDeclarationValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public Dictionary<string, AItemDeclaration> FindDeclarations(ModuleDeclaration moduleDeclaration) {
            var result = new Dictionary<string, AItemDeclaration>();
            var logicalIds = new HashSet<string>();
            moduleDeclaration.InspectNode(node => {
                switch(node) {
                case ParameterDeclaration parameterDeclaration:
                    ValidateItemDeclaration(parameterDeclaration);
                    break;
                case ResourceDeclaration resourceDeclaration:
                    ValidateItemDeclaration(resourceDeclaration);
                    break;
                case FunctionDeclaration functionDeclaration:
                    ValidateItemDeclaration(functionDeclaration);
                    break;
                case VariableDeclaration variableDeclaration:
                    ValidateItemDeclaration(variableDeclaration);
                    break;
                case NestedModuleDeclaration nestedModuleDeclaration:
                    ValidateItemDeclaration(nestedModuleDeclaration);
                    break;
                case ConditionDeclaration conditionDeclaration:
                    ValidateItemDeclaration(conditionDeclaration);
                    break;
                case ImportDeclaration importDeclaration:
                    ValidateItemDeclaration(importDeclaration);
                    break;
                case GroupDeclaration groupDeclaration:
                    ValidateItemDeclaration(groupDeclaration);
                    break;
                case PackageDeclaration packageDeclaration:
                    ValidateItemDeclaration(packageDeclaration);
                    break;
                case MappingDeclaration mappingDeclaration:
                    ValidateItemDeclaration(mappingDeclaration);
                    break;
                case MacroDeclaration macroDeclaration:
                    ValidateItemDeclaration(macroDeclaration);
                    break;
                }
            });
            return result;

            // local functions
            void ValidateItemDeclaration(AItemDeclaration declaration) {

                // check if name is valid
                if(!CloudFormationValidationRules.IsValidCloudFormationName(declaration.ItemName.Value)) {
                    Logger.Log(Error.NameMustBeAlphanumeric, declaration);
                }
                if(CloudFormationValidationRules.IsReservedCloudFormationName(declaration.FullName)) {
                    Logger.Log(ReservedName(declaration.FullName), declaration);
                }

                // check if full name is unique
                if(!result!.TryAdd(declaration.FullName, declaration)) {
                    Logger.Log(Error.DuplicateName(declaration.FullName), declaration);
                }

                // check that logical ID are unambiguous
                if(!logicalIds!.Add(declaration.LogicalId)) {
                    Logger.Log(Error.AmbiguousLogicalId(declaration.FullName));
                }
            }
        }
    }
}