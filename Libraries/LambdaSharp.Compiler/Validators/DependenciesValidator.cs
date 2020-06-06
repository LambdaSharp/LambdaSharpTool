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
using LambdaSharp.Compiler.Model;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.Validators {

    internal sealed class DependenciesValidator : AValidator {

        //--- Constructors ---
        public DependenciesValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public IEnumerable<(ASyntaxNode node, ModuleManifestDependencyType type, string moduleInfo)> FindDependencies(ModuleDeclaration moduleDeclaration) {
            var result = new List<(ASyntaxNode node, ModuleManifestDependencyType type, string moduleInfo)>();
            moduleDeclaration.InspectNode(node => {
                switch(node) {
                case ModuleDeclaration moduleDeclaration:

                    // module have an implicit dependency on LambdaSharp.Core@lambdasharp unless explicitly disabled
                    if(moduleDeclaration.HasModuleRegistration) {
                        result.Add((node, ModuleManifestDependencyType.Shared, "LambdaSharp.Core@lambdasharp"));
                    }
                    break;
                case UsingModuleDeclaration usingModuleDeclaration:

                    // check if module reference is valid
                    if(!ModuleInfo.TryParse(usingModuleDeclaration.ModuleName.Value, out var usingModuleInfo)) {
                        Logger.Log(Error.ModuleAttributeInvalid, usingModuleDeclaration.ModuleName);
                    } else {

                        // default to deployment bucket as origin when missing
                        if(usingModuleInfo.Origin == null) {
                            usingModuleInfo = usingModuleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                        }

                        // add module reference as a shared dependency
                        result.Add((usingModuleDeclaration, ModuleManifestDependencyType.Shared, usingModuleInfo.ToString()));
                    }
                    break;
                case NestedModuleDeclaration nestedModuleDeclaration:

                    // check if module reference is valid
                    if(!ModuleInfo.TryParse(nestedModuleDeclaration.Module?.Value, out var nestedModuleInfo)) {
                        Logger.Log(Error.ModuleAttributeInvalid, (ISyntaxNode?)nestedModuleDeclaration.Module ?? (ISyntaxNode)nestedModuleDeclaration);
                    } else {

                        // default to deployment bucket as origin when missing
                        if(nestedModuleInfo.Origin == null) {
                            nestedModuleInfo = nestedModuleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                        }

                        // add module reference as a nested dependency
                        result.Add((node, ModuleManifestDependencyType.Nested, nestedModuleInfo.ToString()));
                    }
                    break;
                }
            });
            return result;
        }
    }
}