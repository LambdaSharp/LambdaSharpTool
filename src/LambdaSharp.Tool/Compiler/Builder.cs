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
using System.Linq;
using System.Text.RegularExpressions;
using LambdaSharp.Tool.Compiler.Parser.Syntax;

namespace LambdaSharp.Tool.Compiler {

    // TODO:
    //  - record declarations
    //  - import missing information
    //      - other modules manifests
    //      - convert secret key alias to ARN
    //      - cloudformation spec (if need be)
    //  - define custom resource types
    //  - validate usage against imported definitions
    //  - validate nested expressions (done: ValidateExpressionsVisitor)
    //  - create derivative resources
    //  - resolve all references
    //  - validate all !GetAtt occurrences (including those inside a !Sub expression)
    //      - check if this declaration should be typechecked
    //          - if(foundDeclaration.HasTypeValidation) ...
    //          - if(foundDeclaration.HasAttribute(literalExpression.Value)) ...
    //          - LogError($"item '{freeItem.FullName}' of type '{freeItem.Type}' does not have attribute '{attributeName}'");
    //  - add optimization phase that simplifies !Sub statements and removed redundant conditional expressions in !If statements
    //  - the !Ref expression can ONLY reference parameters from within a 'Condition' declaration or
    //      when nested inside an !If expression.
    //  - validate if attribute name exists on resource type (unless type checking is disabled for this declration)
    //  - for !Ref, must know what types of references are legal (Parameters only -or- Resources and Paramaters)
    //  - register custom resource types for the module
    //  - detect cycle between custom resource handler and an instance of the custom resource in its handler
    //  - rename `ItemDeclarations` to `Declarations`
    //  - rename `ParentItemDeclaration` to `ParentDeclaration`
    //  - CloudFormation expression type validation
    //  - tests
    //      - !If with expression in condition
    //      - !If with literal in condition
    //      - condition declaration with reference to non-parameter declaration
    //      - circular dependencies
    //  - warn on unrecognized pragmas
    //  - nested module parameters can only be scalar or list (correct?)
    //  - lambda environment variable values must be scalar or list (correct?)

    public enum XRayTracingLevel {
        Disabled,
        RootModule,
        AllModules
    }

    public class Builder {

        //--- Class Fields ---
        private static Regex ValidResourceNameRegex = new Regex("[a-zA-Z][a-zA-Z0-9]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Fields ---
        private readonly Dictionary<string, AItemDeclaration> _fullNameDeclarations = new Dictionary<string, AItemDeclaration>();
        private readonly HashSet<string> _logicalIds = new HashSet<string>();
        private readonly BuilderReport _report = new BuilderReport();

        //-- Properties ---
        public ModuleDeclaration ModuleDeclaration { get; set; }
        public string ModuleNamespace { get; set; }
        public string ModuleName { get; set; }
        public VersionInfo ModuleVersion { get; set; }

        // TODO: initialize CoreServicesReferenceVersion
        public VersionInfo CoreServicesReferenceVersion { get; private set; }

        public string ModuleFullName => $"{ModuleNamespace}.{ModuleName}";
        public ModuleInfo ModuleInfo => new ModuleInfo(ModuleNamespace, ModuleName, ModuleVersion, origin: ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
        public IEnumerable<AItemDeclaration> ItemDeclarations => _fullNameDeclarations.Values;
        public bool HasErrors => _report.Messages.Any();

        //--- Methods ---
        public bool TryGetItemDeclaration(string fullName, out AItemDeclaration declaration)
            => _fullNameDeclarations.TryGetValue(fullName, out declaration);

        public string AddItemDeclaration(ASyntaxNode parent, AItemDeclaration declaration) {

            // check for reserved names
            if(!ValidResourceNameRegex.IsMatch(declaration.ItemName.Value)) {
                Log(Error.NameMustBeAlphanumeric, declaration);
            } else if(declaration.FullName == "AWS") {
                Log(Error.NameIsReservedAws, declaration);
            }

            // store properties per-node and per-fullname
            if(!_fullNameDeclarations.TryAdd(declaration.FullName, declaration)) {
                Log(Error.DuplicateName, declaration);
            }

            // find a valid CloudFormation logical ID
            var baseLogicalId = declaration.FullName.Replace("::", "");
            var logicalIdSuffix = 0;
            var logicalId = baseLogicalId;
            while(!_logicalIds.Add(logicalId)) {
                ++logicalIdSuffix;
                logicalId = baseLogicalId + logicalIdSuffix;
            }
            return logicalId;
        }

        public void AddSharedDependency(ADeclaration declaration, ModuleInfo moduleInfo) {

            // TODO:
            throw new NotImplementedException();
        }

        public void AddNestedDependency(ADeclaration declaration, ModuleInfo moduleInfo) {

            // TODO:
            throw new NotImplementedException();
        }

        public AExpression GetExportReference(IResourceDeclaration resourceDeclaration) {

            // TODO:
            throw new NotImplementedException();
        }

        public bool IsValidCloudFormationName(string name) => ValidResourceNameRegex.IsMatch(name);

        public void Log(IBuildReportEntry entry, ASyntaxNode node) {
            if(node == null) {
                Log(entry, sourceLocation: null, excact: false);
            } else if(node.SourceLocation != null) {
                Log(entry, node.SourceLocation, excact: true);
            } else {
                var nearestNode = node.Parents.FirstOrDefault(parent => parent.SourceLocation != null);
                if(nearestNode != null) {
                    Log(entry, sourceLocation: nearestNode.SourceLocation, excact: false);
                } else {
                    Log(entry, sourceLocation: null, excact: false);
                }
            }
        }

        public void Log(IBuildReportEntry entry, Parser.SourceLocation sourceLocation, bool excact = true)
            => _report.Add(entry, sourceLocation, excact);
    }
}
