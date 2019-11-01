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

    public class Builder {

        //--- Types ---
        public class DeclarationProperties {

            //--- Properties ---
            public ADeclaration Declaration { get; set; }

            // TODO: this is the CFN expression to use when referencing the declaration; it could be conditional or an attribute, etc.
            public AValueExpression ReferenceExpression { get; set; }

            public List<(string ReferenceName, IEnumerable<AConditionExpression> Conditions, ASyntaxNode Node)> Dependencies { get; set; } = new List<(string, IEnumerable<AConditionExpression>, ASyntaxNode)>();
            public List<ASyntaxNode> ReverseDependencies { get; set; } = new List<ASyntaxNode>();
            public AValueExpression ResolvedValue { get; set; }
        }

        //--- Class Fields ---
        private static Regex ValidResourceNameRegex = new Regex("[a-zA-Z][a-zA-Z0-9]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Fields ---
        private readonly Dictionary<ASyntaxNode, DeclarationProperties> _declarationProperties = new Dictionary<ASyntaxNode, DeclarationProperties>();
        private readonly Dictionary<string, DeclarationProperties> _fullNameProperties = new Dictionary<string, DeclarationProperties>();
        private readonly List<string> _messages = new List<string>();

        //-- Properties ---
        public string ModuleNamespace { get; set; }
        public string ModuleName { get; set; }
        public VersionInfo ModuleVersion { get; set; }

        //--- Methods ---
        public DeclarationProperties GetProperties(ADeclaration declaration) {
            _declarationProperties.TryGetValue(declaration, out var properties);
            return properties;
        }

        public bool TryGetProperties(ADeclaration declaration, out DeclarationProperties properties)
            => _declarationProperties.TryGetValue(declaration, out properties);

        public DeclarationProperties GetProperties(string fullName) {
            _fullNameProperties.TryGetValue(fullName, out var properties);
            return properties;
        }

        public bool TryGetProperties(string fullName, out DeclarationProperties properties) =>
            _fullNameProperties.TryGetValue(fullName, out properties);

        public Builder.DeclarationProperties AddItemDeclaration(ASyntaxNode parent, AItemDeclaration declaration) {
            var properties = new Builder.DeclarationProperties {
                Declaration = declaration
            };

            // check for reserved names
            if(!ValidResourceNameRegex.IsMatch(declaration.LocalName)) {
                LogError($"name must be alphanumeric", declaration.SourceLocation);
            } else if(declaration.FullName == "AWS") {
                LogError($"AWS is a reserved name", declaration.SourceLocation);
            }

            // store properties per-node and per-fullname
            _declarationProperties.Add(declaration, properties);
            _fullNameProperties.Add(declaration.FullName, properties);
            return properties;
        }

        public void AddSharedDependency(ModuleInfo moduleInfo) {

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

        public AValueExpression GetReference(AItemDeclaration itemDeclaration) {

            // TODO:
            throw new NotImplementedException();
        }

        public bool IsValidCloudFormationName(string name) => ValidResourceNameRegex.IsMatch(name);

        public void LogError(string message, SourceLocation location)
            => _messages.Add($"ERROR: {message} @ {location?.FilePath ?? "n/a"}({location?.LineNumberStart ?? 0},{location?.ColumnNumberStart ?? 0})");
    }
}
