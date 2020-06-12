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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.CloudFormation;
using LambdaSharp.CloudFormation.Template;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Parser;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.TypeSystem;
using LambdaSharp.Compiler.Validators;

namespace LambdaSharp.Compiler {

    public interface IModuleScopeDependencyProvider {

        //--- Properties ---
        ILogger Logger { get; }

        //--- Methods ---
        string ReadFile(string filePath);
    }

    public class ModuleScope : IModuleValidatorDependencyProvider, ILambdaSharpParserDependencyProvider {

        //--- Class Methods ---
        public static bool TryParseModuleFullName(string compositeModuleFullName, out string moduleNamespace, out string moduleName) {
            moduleNamespace = "<BAD>";
            moduleName = "<BAD>";
            if(!ModuleInfo.TryParse(compositeModuleFullName, out var moduleInfo)) {
                return false;
            }
            if((moduleInfo.Version != null) || (moduleInfo.Origin != null)) {
                return false;
            }
            moduleNamespace = moduleInfo.Namespace;
            moduleName = moduleInfo.Name;
            return true;
        }

        //--- Fields ---
        private Dictionary<string, AItemDeclaration> _declarations = new Dictionary<string, AItemDeclaration>();

        //--- Constructors ---
        public ModuleScope(IModuleScopeDependencyProvider provider)
            => Provider = provider ?? throw new ArgumentNullException(nameof(provider));

        //--- Properties ---
        private IModuleScopeDependencyProvider Provider { get; }
        private ILogger Logger => Provider.Logger;
        private string? ModuleNamespace { get; set; }
        private string? ModuleName { get; set; }
        private VersionInfo? ModuleVersion { get; set; }

        //--- Methods ---
        public async Task<CloudFormationTemplate?> CompileAsync(string filePath) {
            var moduleDeclaration = LoadModule(filePath);
            if(moduleDeclaration == null) {
                return null;
            }
            ValidateModuleInformation(moduleDeclaration);

            // validate AST integrity
            new IntegrityValidator(this).Validate(moduleDeclaration);

            // find module dependencies
            var dependencies = new DependenciesValidator(this).FindDependencies(moduleDeclaration);
            var cloudformationSpec = moduleDeclaration.CloudFormation;

            // TODO: download external dependencies

            // validate declarations
            new ParameterDeclarationValidator(this).Validate(moduleDeclaration);
            new ResourceDeclarationValidator(this).Validate(moduleDeclaration);

            // register local resource types
            var localResourceTypes = new ResourceTypeDeclarationValidator(this).FindResourceTypes(moduleDeclaration);

            // register all declarations
            new ItemDeclarationValidator(this).Validate(moduleDeclaration);

            // evaluate expressions
            new ConstantExpressionEvaluator(this).Evaluate(moduleDeclaration);
            new ReferenceValidator(this).Validate(moduleDeclaration, _declarations);

            // TODO: annotate expression types
            // TODO: ensure that constructed resources have all required properties
            // TODO: ensure that referenced attributes exist

            // ensure that handler references are valid
            new ResourceTypeHandlerValidator(this).Validate(moduleDeclaration);
            new MacroHandlerValidator(this).Validate(moduleDeclaration);

            // TODO: needs access to IAM permissions
            new AllowValidator(this).Validate(moduleDeclaration);

            // validate resource scopes
            new ScopeValidator(this).Validate(moduleDeclaration, _declarations);

            // optimize AST
            new ExpressionOptimization(this).Optimize(moduleDeclaration);

            // resolve secrets
            await new EmbeddedSecretsResolver(this).ResolveAsync(moduleDeclaration);

            throw new NotImplementedException();
        }

        private ModuleDeclaration? LoadModule(string filePath) {

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

       private void ValidateModuleInformation(ModuleDeclaration moduleDeclaration) {

            // ensure module version is present and valid
            if(!VersionInfo.TryParse(moduleDeclaration.Version.Value, out var version)) {
                Logger.Log(Error.VersionAttributeInvalid, moduleDeclaration.Version);
                version = VersionInfo.Parse("0.0");
            }
            if(ModuleVersion == null) {
                ModuleVersion = version;
            }

            // ensure module has a namespace and name
            if(TryParseModuleFullName(moduleDeclaration.ModuleName.Value, out string moduleNamespace, out var moduleName)) {
                ModuleNamespace = moduleNamespace;
                ModuleName = moduleName;
            } else {
                Logger.Log(Error.ModuleNameAttributeInvalid, moduleDeclaration.ModuleName);
            }
        }

        //--- IModuleValidatorDependencyProvider Members ---
        ILogger IModuleValidatorDependencyProvider.Logger => Logger;

        bool IModuleValidatorDependencyProvider.TryGetResourceType(string typeName, [NotNullWhen(true)] out IResourceType? resourceType) {

            // TODO:
            throw new NotImplementedException();
        }

        Task<string> IModuleValidatorDependencyProvider.ConvertKmsAliasToArn(string alias) {

            // TODO:
            throw new NotImplementedException();
        }

        void IModuleValidatorDependencyProvider.DeclareItem(AItemDeclaration declaration)
            => _declarations.Add(declaration.FullName, declaration);

        bool IModuleValidatorDependencyProvider.TryGetItem(string fullname, [NotNullWhen(true)] out AItemDeclaration? itemDeclaration)
            => _declarations.TryGetValue(fullname, out itemDeclaration);

        //--- ILambdaSharpParserDependencyProvider Members ---
        ILogger ILambdaSharpParserDependencyProvider.Logger => Logger;

        string ILambdaSharpParserDependencyProvider.ReadFile(string filePath) => Provider.ReadFile(filePath);
    }
}