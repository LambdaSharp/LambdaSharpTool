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
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.CloudFormation;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Parser;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Validators;

namespace LambdaSharp.Compiler {

    public interface IModuleScopeDependencyProvider {

        //--- Properties ---
        ILogger Logger { get; }

        //--- Methods ---
        string ReadFile(string filePath);

    }

    public class ModuleScope : IModuleValidatorDependencyProvider, ILambdaSharpParserDependencyProvider {

        //--- Constructors ---
        public ModuleScope(IModuleScopeDependencyProvider provider)
            => Provider = provider ?? throw new ArgumentNullException(nameof(provider));

        //--- Properties ---
        private IModuleScopeDependencyProvider Provider { get; }
        private ILogger Logger => Provider.Logger;

        //--- Methods ---
        public async Task<CloudFormationTemplate?> ComileAsync(string filePath) {
            var module = Load(filePath);
            if(module == null) {
                return null;
            }

            // TODO: validate module name

            // find module dependencies
            var dependencies = new DependenciesValidator(this).FindDependencies(module);
            var cloudformationSpec = module.CloudFormation;

            // TODO: download external dependencies

            // normalize AST for analysis
            new ExpressionNormalization(this).Normalize(module);

            // validate declarations
            new ParameterDeclarationValidator(this).Validate(module);
            new ResourceDeclarationValidator(this).Validate(module);
            new AllowValidator(this).Validate(module);

            // register local resource types
            var localResourceTypes = new ResourceTypeDeclarationValidator(this).FindResourceTypes(module);

            // ensure that all references can be resolved
            var declarations = new ItemDeclarationValidator(this).FindDeclarations(module);
            new ReferenceValidator(this).Validate(module, declarations);

            // TODO: annotate expression types
            // TODO: ensure that constructed resources have all required properties
            // TODO: ensure that referenced attributes exist

            // ensure that handler references are valid
            new ResourceTypeHandlerValidator(this).Validate(module, declarations);
            new MacroHandlerValidator(this).Validate(module, declarations);

            // validate resource scopes
            new ScopeValidator(this).Validate(module, declarations);

            // optimize AST
            new ExpressionOptimization(this).Optimize(module);

            throw new NotImplementedException();
        }

        private ModuleDeclaration? Load(string filePath) {

            // load specified module
            var result = new LambdaSharpParser(this, filePath).ParseModule();
            if(result == null) {
                return null;
            }

            // load LambdaSharp declarations if required
            if(result.HasLambdaSharpDependencies) {
                var lambdasharp = new LambdaSharpParser(this, "LambdaSharp.Compiler.dll", "LambdaSharp-Module.yml").ParseModule();
                if(lambdasharp == null) {
                    throw new ShouldNeverHappenException("unable to parse LambdaSharp.Compiler.dll/LambdaSharp-Module.yml");
                }
                result.Items.AddRange(lambdasharp.Items);
            }

            // load standard declarations
            var standard = new LambdaSharpParser(this, "LambdaSharp.Compiler.dll", "Standard-Module.yml").ParseModule();
            if(standard == null) {
                throw new ShouldNeverHappenException("unable to parse LambdaSharp.Compiler.dll/Standard-Module.yml");
            }
            result.Items.AddRange(standard.Items);

            // add secrets as Module::Secrets
            if(result.Secrets.Any()) {

                // TODO:
            }
            return result;
       }

        //--- IModuleValidatorDependencyProvider Members ---
        ILogger IModuleValidatorDependencyProvider.Logger => Logger;

        bool IModuleValidatorDependencyProvider.IsValidResourceType(string type) {

            // TODO:
            return true;
        }

        bool IModuleValidatorDependencyProvider.TryGetResourceType(string typeName, out ResourceType resourceType) {

            // TODO:
            throw new NotImplementedException();
        }

        //--- ILambdaSharpParserDependencyProvider Members ---
        ILogger ILambdaSharpParserDependencyProvider.Logger => Logger;

        string ILambdaSharpParserDependencyProvider.ReadFile(string filePath) => Provider.ReadFile(filePath);
    }
}