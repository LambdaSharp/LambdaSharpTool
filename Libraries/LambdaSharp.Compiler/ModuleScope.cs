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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using LambdaSharp.CloudFormation.Template;
using LambdaSharp.CloudFormation.TypeSystem;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Parser;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.SyntaxProcessors;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Modules;
using LambdaSharp.Modules.Metadata;

namespace LambdaSharp.Compiler {

    public interface IModuleScopeDependencyProvider {

        //--- Properties ---
        ILogger Logger { get; }

        //--- Methods ---
        string ReadFile(string filePath);
        Task<ITypeSystem> LoadCloudFormationSpecificationAsync(string region);
    }

    public sealed class ModuleScope : ISyntaxProcessorDependencyProvider, ILambdaSharpParserDependencyProvider {

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
        private Dictionary<string, AExpression> _referenceExpressions = new Dictionary<string, AExpression>();
        private Dictionary<string, AExpression> _valueExpressions = new Dictionary<string, AExpression>();

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
            new SyntaxTreeIntegrityProcessor(this).ValidateIntegrity(moduleDeclaration);

            // resolve all external module dependencies
            await new ExternalDependenciesProcessor(this).ResolveDependenciesAsync(moduleDeclaration);

            // register pseudo-parameter and module declarations
            new PseudoParameterProcessor(this).Declare(moduleDeclaration);
            new ItemDeclarationProcessor(this).Declare(moduleDeclaration);

            // process declarations
            new ParameterAndImportDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new ResourceDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new PackageDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new MappingDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new FunctionDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new VariableDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new StackDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new ResourceTypeDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new AppDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);
            new SecretTypeDeclarationProcessor(this).ValidateDeclaration(moduleDeclaration);

            // TODO: needs access to IAM permissions
            new AllowProcessor(this).ValidateDeclaration();

            // register local resource types
            var localResourceTypes = new ResourceTypeDeclarationProcessor(this).FindResourceTypes(moduleDeclaration);

            // TODO: validate expression nesting

            // all declarations have been made; !IsDefined can now be evaluated
            new IsDefinedProcessor(this).Evaluate();

            // TODO: inject compilation constants (module name, version, etc.)

            // evaluate expressions
            new ExpressionEvaluator(this).Normalize();
            new ReferentialIntegrityValidator(this).Validate();
            new ExpressionTypeProcessor(this).Process();

            // ensure that constructed resources have required and necessary properties
            new ResourceInitializationValidator(this).ValidateExpressions();

            // find all resource dependencies for the 'Finalizer' invocation
            new FinalizerDependenciesProcessor(this).Process();

            // resolve secrets
            await new EmbeddedSecretsProcessor(this).ProcessAsync(moduleDeclaration);

            // TODO: remove any unused resources that can be garbage collected

            // optimize expressions
            new ExpressionEvaluator(this).Evaluate();

            // TODO: generate cloudformation template
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

                // TODO: add secrets as Module::Secrets
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
        ILogger ISyntaxProcessorDependencyProvider.Logger => Logger;
        IEnumerable<AItemDeclaration> ISyntaxProcessorDependencyProvider.Declarations => _declarations.Values;

        bool ISyntaxProcessorDependencyProvider.TryGetResourceType(string typeName, [NotNullWhen(true)] out IResourceType? resourceType) {

            // TODO:
            throw new NotImplementedException();
        }

        Task<string> ISyntaxProcessorDependencyProvider.ConvertKmsAliasToArn(string alias) {

            // TODO:
            throw new NotImplementedException();
        }

        void ISyntaxProcessorDependencyProvider.DeclareItem(ASyntaxNode? parent, AItemDeclaration itemDeclaration) {
            if(itemDeclaration == null) {
                throw new ArgumentNullException(nameof(itemDeclaration));
            }
            if(parent == null) {
                throw new ArgumentNullException(nameof(parent));
            }
            parent.Adopt(itemDeclaration);
            _declarations.Add(itemDeclaration.FullName, itemDeclaration);
        }

        bool ISyntaxProcessorDependencyProvider.TryGetItem(string fullname, [NotNullWhen(true)] out AItemDeclaration? itemDeclaration)
            => _declarations.TryGetValue(fullname, out itemDeclaration);

        //--- ILambdaSharpParserDependencyProvider Members ---
        ILogger ILambdaSharpParserDependencyProvider.Logger => Logger;

        VersionInfo ISyntaxProcessorDependencyProvider.CoreServicesReferenceVersion => throw new NotImplementedException();

        string ILambdaSharpParserDependencyProvider.ReadFile(string filePath) => Provider.ReadFile(filePath);

        Task<ModuleManifest> ISyntaxProcessorDependencyProvider.ResolveModuleInfoAsync(ModuleManifestDependencyType dependencyType, ModuleInfo moduleInfo)
            => throw new NotImplementedException();

        void ISyntaxProcessorDependencyProvider.DeclareReferenceExpression(string fullname, AExpression expression)
            => _referenceExpressions[fullname] = expression;

        void ISyntaxProcessorDependencyProvider.DeclareValueExpression(string fullname, AExpression expression)
            => _valueExpressions[fullname] = expression;

        bool ISyntaxProcessorDependencyProvider.TryGetReferenceExpression(string fullname, [NotNullWhen(true)] out AExpression? expression)
            => _referenceExpressions.TryGetValue(fullname, out expression);

        bool ISyntaxProcessorDependencyProvider.TryGetValueExpression(string fullname, [NotNullWhen(true)] out AExpression? expression)
            => _valueExpressions.TryGetValue(fullname, out expression);

        Task<ITypeSystem> ISyntaxProcessorDependencyProvider.LoadCloudFormationSpecificationAsync(string region) {

            // TODO:
            throw new NotImplementedException();
        }

        void ISyntaxProcessorDependencyProvider.AddTypeSystem(ITypeSystem typeSystem) {

            // TODO:
            throw new NotImplementedException();
        }
    }
}