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
using System.Threading.Tasks;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.EventSources;
using LambdaSharp.Modules;
using LambdaSharp.Modules.Metadata;
using LambdaSharp.Modules.Metadata.TypeSystem;

namespace LambdaSharp.Compiler.SyntaxProcessors {
    using ErrorFunc = Func<string, Error>;

    /// <summary>
    /// The <see cref="ExternalDependenciesProcessor"/> class is responsible for find all module dependencies and resolving
    /// them before processing starts.
    /// </summary>
    internal sealed class ExternalDependenciesProcessor : ASyntaxProcessor {

        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly Error ModuleAttributeInvalidModuleInfo = new Error("'Module' attribute must be a module reference");
        private static readonly ErrorFunc ModuleNotFound = parameter => new Error($"could not resolve module {parameter}");
        #endregion

        //--- Constructors ---
        public ExternalDependenciesProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public async Task ResolveDependenciesAsync(ModuleDeclaration moduleDeclaration) {

            // discover all dependencies
            await moduleDeclaration.InspectAsync(async node => {
                switch(node) {
                case ModuleDeclaration moduleDeclaration:

                    // load CloudFormation specification
                    Provider.AddTypeSystem(await Provider.LoadCloudFormationSpecificationAsync(
                        moduleDeclaration.CloudFormation?.Region?.Value ?? "us-east-1"
                    ));

                    // modules have an implicit dependency on LambdaSharp.Core@lambdasharp unless explicitly disabled
                    var lambdaSharpModule = new ModuleInfo("LambdaSharp", "Core", version: Provider.CoreServicesReferenceVersion, "lambdasharp");
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
                case StackDeclaration nestedStackDeclaration:

                    // check if module reference is valid
                    if(!ModuleInfo.TryParse(nestedStackDeclaration.Module?.Value, out var nestedStackModuleInfo)) {
                        Logger.Log(ModuleAttributeInvalidModuleInfo, (ISyntaxNode?)nestedStackDeclaration.Module ?? nestedStackDeclaration);
                    } else if(!await ValidateModuleReferenceAsync(ModuleManifestDependencyType.Nested, nestedStackModuleInfo)) {
                        Logger.Log(ModuleNotFound(nestedStackModuleInfo.ToString()), (ASyntaxNode?)nestedStackDeclaration.Module ?? nestedStackDeclaration);
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
                case S3EventSourceDeclaration s3EventSourceDeclaration:

                    // S3 event source has an implicit dependency on LambdaSharp.S3.Subscriber@lambdasharp
                    var lambdaSharpS3SubscriberModule = new ModuleInfo("LambdaSharp", "S3.Subscriber", version: Provider.CoreServicesReferenceVersion, "lambdasharp");
                    if(!await ValidateModuleReferenceAsync(ModuleManifestDependencyType.Shared, lambdaSharpS3SubscriberModule)) {
                        Logger.Log(ModuleNotFound(lambdaSharpS3SubscriberModule.ToString()), s3EventSourceDeclaration);
                    }
                    break;
                }
            });

            // local functions
            async Task<bool> ValidateModuleReferenceAsync(ModuleManifestDependencyType dependencyType, ModuleInfo moduleInfo) {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {

                    // TODO: how does this help to resolve the module?
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // default to the lowest possible version number
                if(moduleInfo.Version == null) {
                    moduleInfo = moduleInfo.WithVersion(VersionInfo.Parse("0.0.0"));
                }
                var manifest = await Provider.ResolveModuleInfoAsync(dependencyType, moduleInfo);
                if((manifest?.ModuleInfo != null) && (dependencyType == ModuleManifestDependencyType.Shared)) {
                    Provider.AddTypeSystem(new ModuleManifestTypeSystem(manifest.ModuleInfo.ToString(), manifest));
                }
                return manifest != null;
            }
        }
    }
}