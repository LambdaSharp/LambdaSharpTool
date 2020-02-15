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

using LambdaSharp.Tool.Compiler.Parser.Syntax;
using LambdaSharp.Tool.Model;

namespace LambdaSharp.Tool.Compiler.Analyzers {

    public class DiscoverDependenciesAnalyzer : ASyntaxAnalyzer {

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public DiscoverDependenciesAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override void VisitStart(ASyntaxNode? parent, ModuleDeclaration node) {
            if(node.HasModuleRegistration) {

                // add module reference as a shared dependency
                _builder.AddDependencyAsync(
                    new ModuleInfo("LambdaSharp", "Core", _builder.CoreServicesReferenceVersion, "lambdasharp"),
                    ModuleManifestDependencyType.Shared,
                    node: null
                ).Wait();
            }

            // TODO: we also need to discover what CloudFormation schema version/region is expected
        }

        public override void VisitStart(ASyntaxNode? parent, UsingModuleDeclaration node) {

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.ModuleName.Value, out var moduleInfo)) {
                _builder.Log(Error.ModuleAttributeInvalid, node.ModuleName);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // add module reference as a shared dependency
                _builder.AddDependencyAsync(moduleInfo, ModuleManifestDependencyType.Shared, node.ModuleName).Wait();
            }
        }

        public override void VisitStart(ASyntaxNode? parent, NestedModuleDeclaration node) {

            // check if module reference is valid
            if(!ModuleInfo.TryParse(node.Module?.Value, out var moduleInfo)) {
                _builder.Log(Error.ModuleAttributeInvalid, node.Module);
            } else {

                // default to deployment bucket as origin when missing
                if(moduleInfo.Origin == null) {
                    moduleInfo = moduleInfo.WithOrigin(ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
                }

                // add module reference as a nested dependency
                _builder.AddDependencyAsync(moduleInfo, ModuleManifestDependencyType.Nested, node.Module).Wait();
            }
        }
    }
}
