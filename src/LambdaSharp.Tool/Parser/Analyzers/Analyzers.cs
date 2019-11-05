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
using System.Text.RegularExpressions;
using LambdaSharp.Tool.Parser.Syntax;

namespace LambdaSharp.Tool.Parser.Analyzers {

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
        private readonly List<string> _messages = new List<string>();

        //-- Properties ---
        public string ModuleNamespace { get; set; }
        public string ModuleName { get; set; }
        public VersionInfo ModuleVersion { get; set; }

        // TODO: initialize CoreServicesReferenceVersion
        public VersionInfo CoreServicesReferenceVersion { get; private set; }

        public string ModuleFullName => $"{ModuleNamespace}.{ModuleName}";
        public ModuleInfo ModuleInfo => new ModuleInfo(ModuleNamespace, ModuleName, ModuleVersion, origin: ModuleInfo.MODULE_ORIGIN_PLACEHOLDER);
        public IEnumerable<AItemDeclaration> ItemDeclarations => _fullNameDeclarations.Values;

        //--- Methods ---
        public bool TryGetItemDeclaration(string fullName, out AItemDeclaration declaration)
            => _fullNameDeclarations.TryGetValue(fullName, out declaration);

        public void AddItemDeclaration(ASyntaxNode parent, AItemDeclaration declaration) {

            // check for reserved names
            if(!ValidResourceNameRegex.IsMatch(declaration.LocalName)) {
                LogError($"name must be alphanumeric", declaration.SourceLocation);
            } else if(declaration.FullName == "AWS") {
                LogError($"AWS is a reserved name", declaration.SourceLocation);
            }

            // store properties per-node and per-fullname
            _fullNameDeclarations.Add(declaration.FullName, declaration);
        }

        public void AddSharedDependency(ADeclaration declaration, ModuleInfo moduleInfo) {

            // TODO:
            throw new NotImplementedException();
        }

        public void AddNestedDependency(ModuleInfo moduleInfo) {

            // TODO:
            throw new NotImplementedException();
        }

        public AValueExpression GetExportReference(ResourceDeclaration resourceDeclaration) {

            // TODO:
            throw new NotImplementedException();
        }

        public bool IsValidCloudFormationName(string name) => ValidResourceNameRegex.IsMatch(name);

        // TODO: errors needs an error number and fixed string
        public void LogError(string message, SourceLocation location)
            => _messages.Add($"ERROR: {message} @ {location?.FilePath ?? "n/a"}({location?.LineNumberStart ?? 0},{location?.ColumnNumberStart ?? 0})");
    }
}
