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
using LambdaSharp.CloudFormation.Template;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Parser;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Compiler.TypeSystem;
using LambdaSharp.Compiler.Processors;

namespace LambdaSharp.Compiler {

    public interface IModuleScopeDependencyProvider {

        //--- Properties ---
        ILogger Logger { get; }

        //--- Methods ---

        // TODO: this should be Async and maybe return a Stream instead?
        string ReadFile(string filePath);
        Task<ITypeSystem> LoadCloudFormationSpecificationAsync(string region, string version);
    }

    public sealed class ModuleScope : IProcessorDependencyProvider, ILambdaSharpParserDependencyProvider {

        // TODO:
        //  - validate usage against imported definitions
        //  - detect cycle between custom resource handler and an instance of the custom resource in its handler
        //  - register custom resource types for the module
        //  - warn on unrecognized pragmas
        //  - nested module parameters can only be scalar or list (correct?)
        //  - lambda environment variable values must be scalar or list (correct?)

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
        private ITypeSystem CloudFormationSpec { get; set; } = new EmptyTypeSystem();

        //--- Methods ---
        public async Task<CloudFormationTemplate?> CompileAsync(string filePath) {
            var moduleDeclaration = LoadModule(filePath);
            if(moduleDeclaration == null) {
                return null;
            }
            ValidateModuleInformation(moduleDeclaration);

            // validate AST integrity
            new IntegrityProcessor(this).Validate(moduleDeclaration);

            // find module dependencies
            var dependencies = new DependenciesProcessor(this).FindDependencies(moduleDeclaration);

            // TODO: download external dependencies

            // load CloudFormation specification
            CloudFormationSpec = await Provider.LoadCloudFormationSpecificationAsync(
                moduleDeclaration.CloudFormation?.Region?.Value ?? "us-east-1",
                moduleDeclaration.CloudFormation?.Version?.Value ?? "15.0.0"
            );

            // register all declarations
            new PseudoParameterProcessor(this).Validate(moduleDeclaration);
            new ItemDeclarationProcessor(this).Validate(moduleDeclaration);

            // validate declarations
            new ParameterDeclarationProcessor(this).Validate(moduleDeclaration);
            new ResourceDeclarationProcessor(this).Validate(moduleDeclaration);

            // register local resource types
            var localResourceTypes = new ResourceTypeDeclarationProcessor(this).FindResourceTypes(moduleDeclaration);

            // evaluate expressions
            new ConstantExpressionEvaluator(this).Evaluate(moduleDeclaration);
            new ReferenceProcessor(this).Validate(moduleDeclaration);

            // TODO: annotate expression types
            // TODO: ensure that constructed resources have all required properties
            // TODO: ensure that referenced attributes exist

            // ensure that handler references are valid
            new ResourceTypeHandlerProcessor(this).Validate(moduleDeclaration);
            new MacroHandlerProcessor(this).Validate(moduleDeclaration);

            // TODO: needs access to IAM permissions
            new AllowProcessor(this).Validate(moduleDeclaration);

            // validate resource scopes
            new ScopeProcessor(this).Validate(moduleDeclaration);

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

        //--- IModuleProcessorDependencyProvider Members ---
        ILogger IProcessorDependencyProvider.Logger => Logger;
        IEnumerable<AItemDeclaration> IProcessorDependencyProvider.Declarations => _declarations.Values;

        bool IProcessorDependencyProvider.TryGetResourceType(string typeName, [NotNullWhen(true)] out IResourceType? resourceType) {

            // TODO:
            throw new NotImplementedException();
        }

        Task<string> IProcessorDependencyProvider.ConvertKmsAliasToArn(string alias) {

            // TODO:
            throw new NotImplementedException();
        }

        void IProcessorDependencyProvider.DeclareItem(AItemDeclaration declaration)
            => _declarations.Add(declaration.FullName, declaration);

        bool IProcessorDependencyProvider.TryGetItem(string fullname, [NotNullWhen(true)] out AItemDeclaration? itemDeclaration)
            => _declarations.TryGetValue(fullname, out itemDeclaration);

        //--- ILambdaSharpParserDependencyProvider Members ---
        ILogger ILambdaSharpParserDependencyProvider.Logger => Logger;

        string ILambdaSharpParserDependencyProvider.ReadFile(string filePath) => Provider.ReadFile(filePath);
    }
}