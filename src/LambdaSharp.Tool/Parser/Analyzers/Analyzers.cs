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

using System.Collections.Generic;
using LambdaSharp.Tool.Parser.Syntax;

namespace LambdaSharp.Tool.Parser.Analyzers {

    public class Builder {

        //--- Types ---
        public class DeclarationProperties {

            //--- Properties ---
            public ADeclaration Declaration { get; set; }
            public string FullName { get; set; }
            public List<(string ReferenceName, IEnumerable<AConditionExpression> Conditions, ASyntaxNode Node)> Dependencies { get; set; } = new List<(string, IEnumerable<AConditionExpression>, ASyntaxNode)>();
            public List<ASyntaxNode> ReverseDependencies { get; set; } = new List<ASyntaxNode>();
            public AValueExpression ResolvedValue { get; set; }
        }

        //--- Fields ---
        private readonly Dictionary<ASyntaxNode, DeclarationProperties> _declarationProperties = new Dictionary<ASyntaxNode, DeclarationProperties>();
        private readonly Dictionary<string, DeclarationProperties> _fullNameProperties = new Dictionary<string, DeclarationProperties>();
        private readonly List<string> _messages = new List<string>();

        //-- Properties ---
        public IEnumerable<string> Messages => _messages;

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

        public void DefineDeclaration(ASyntaxNode parent, ADeclaration declaration, string name) {
            var properties = new Builder.DeclarationProperties {
                Declaration = declaration
            };

            // assign full hierarchical name
            if(_declarationProperties.TryGetValue(parent, out var parentProperties)) {
                properties.FullName = $"{parentProperties.FullName}::{name}";
            } else {
                properties.FullName = name;
            }

            // store properties per-node and per-fullname
            _declarationProperties.Add(declaration, properties);
            _fullNameProperties.Add(properties.FullName, properties);
        }

        public void LogError(string message, SourceLocation location)
            => _messages.Add($"ERROR: {message} @ {location.FilePath}({location.LineNumberStart},{location.ColumnNumberStart})");
    }

    public class DeclarationsAnalyzer : ASyntaxVisitor {

        //--- Fields ---
        private readonly Builder _builder;

        //--- Constructors ---
        public DeclarationsAnalyzer(Builder builder) => _builder = builder ?? throw new System.ArgumentNullException(nameof(builder));

        //--- Methods ---
        public override void VisitStart(ASyntaxNode parent, ParameterDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Parameter.Value);
        }

        public override void VisitStart(ASyntaxNode parent, VariableDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Variable.Value);
        }

        public override void VisitStart(ASyntaxNode parent, GroupDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Group.Value);
        }

        public override void VisitStart(ASyntaxNode parent, ConditionDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Condition.Value);
        }

        public override void VisitStart(ASyntaxNode parent, ResourceDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Resource.Value);
        }

        public override void VisitStart(ASyntaxNode parent, NestedModuleDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Nested.Value);
        }

        public override void VisitStart(ASyntaxNode parent, PackageDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Package.Value);
        }

        public override void VisitStart(ASyntaxNode parent, FunctionDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Function.Value);
        }

        public override void VisitStart(ASyntaxNode parent, MappingDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Mapping.Value);
        }

        public override void VisitStart(ASyntaxNode parent, ResourceTypeDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.ResourceType.Value);
        }

        public override void VisitStart(ASyntaxNode parent, MacroDeclaration node) {
            _builder.DefineDeclaration(parent, node, node.Macro.Value);
        }
    }
}
