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
using System.Threading.Tasks;
using LambdaSharp.Compiler.Model;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.SyntaxProcessors {
    using ErrorFunc = Func<string, Error>;

    internal sealed class ExternalDependenciesProcessor : ASyntaxProcessor {

        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly Error ModuleAttributeInvalidModuleInfo = new Error(0, "'Module' attribute must be a module reference");
        private static readonly ErrorFunc ModuleNotFound = parameter => new Error(0, $"could not resolve module {parameter}");
        #endregion

        //--- Constructors ---
        public ExternalDependenciesProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public async Task ProcessAsync(ModuleDeclaration moduleDeclaration) {

            // TODO: we should be a bit smarter to reduce the number of calls to ResolveModuleInfoAsync()

            // discover all dependencies
            await moduleDeclaration.InspectAsync(async node => {
                switch(node) {
                case ModuleDeclaration moduleDeclaration:

                    // modules have an implicit dependency on LambdaSharp.Core@lambdasharp unless explicitly disabled
                    var lambdaSharpModule = new ModuleInfo("LambdaSharp", "Core", version: null, "lambdasharp");
                    if(moduleDeclaration.HasModuleRegistration && !await ValidateModuleReferenceAsync(ModuleManifestDependencyType.Shared, lambdaSharpModule)) {
                        Logger.Log(ModuleNotFound(lambdaSharpModule.ToString()), moduleDeclaration);
                    }
                    break;
                case UsingModuleDeclaration usingModuleDeclaration:

                    // check if module reference is valid
                    if(!ModuleInfo.TryParse(usingModuleDeclaration.ModuleName.Value, out var usingModuleInfo)) {
                        Logger.Log(ModuleAttributeInvalidModuleInfo, usingModuleDeclaration.ModuleName);
                    } else if(!await ValidateModuleReferenceAsync(ModuleManifestDependencyType.Shared, usingModuleInfo)) {
                        Logger.Log(ModuleNotFound(usingModuleInfo.ToString()), usingModuleDeclaration.ModuleName);
                    }
                    break;
                case NestedModuleDeclaration nestedModuleDeclaration:

                    // check if module reference is valid
                    if(!ModuleInfo.TryParse(nestedModuleDeclaration.Module?.Value, out var nestedModuleInfo)) {
                        Logger.Log(ModuleAttributeInvalidModuleInfo, (ISyntaxNode?)nestedModuleDeclaration.Module ?? nestedModuleDeclaration);
                    } else if(!await ValidateModuleReferenceAsync(ModuleManifestDependencyType.Nested, nestedModuleInfo)) {
                        Logger.Log(ModuleNotFound(nestedModuleInfo.ToString()), (ASyntaxNode?)nestedModuleDeclaration.Module ?? nestedModuleDeclaration);
                    }
                    break;
                case ImportDeclaration importDeclaration:
                    importDeclaration.GetModuleAndExportName(out var importModuleName, out var importExportName);

                    // check if module reference is valid
                    if(!ModuleInfo.TryParse(importModuleName, out var importModuleInfo)) {
                        Logger.Log(ModuleAttributeInvalidModuleInfo, (ISyntaxNode?)importDeclaration.Module ?? importDeclaration);
                    } else if(!await ValidateModuleReferenceAsync(ModuleManifestDependencyType.Shared, importModuleInfo)) {
                        Logger.Log(ModuleNotFound(importModuleInfo.ToString()), (ASyntaxNode?)importDeclaration.Module ?? importDeclaration);
                    }
                    break;
                }
            });

            // local functions
            async Task<bool> ValidateModuleReferenceAsync(ModuleManifestDependencyType dependencyType, ModuleInfo moduleInfo) {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // default to the lowest possible version number
                if(moduleInfo.Version == null) {
                    moduleInfo = moduleInfo.WithVersion(VersionInfo.Parse("0.0.0"));
                }
                var manifest = await Provider.ResolveModuleInfoAsync(dependencyType, moduleInfo);
                return manifest != null;
            }
        }
    }
}