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
using System.Linq;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Resource")]
    public sealed class ResourceDeclaration :
        AItemDeclaration,
        IScopedDeclaration,
        IResourceDeclaration
    {

        //--- Fields ---
        private AExpression? _if;
        private LiteralExpression? _type;
        private SyntaxNodeCollection<LiteralExpression> _scope;
        private SyntaxNodeCollection<LiteralExpression>? _allow;
        private AExpression? _value;
        private SyntaxNodeCollection<LiteralExpression> _dependsOn;
        private ObjectExpression _properties;
        private LiteralExpression? _defaultAttribute;
        private ListExpression _pragmas;
        private LiteralExpression? _deletionPolicy;

        //--- Constructors ---
        public ResourceDeclaration(LiteralExpression itemName) : base(itemName) {
            _scope = Adopt(new SyntaxNodeCollection<LiteralExpression>());
            _dependsOn = Adopt(new SyntaxNodeCollection<LiteralExpression>());
            _properties = Adopt(new ObjectExpression());
            _pragmas = Adopt(new ListExpression());
        }

        //--- Properties ---

        [SyntaxOptional]
        public AExpression? If {
            get => _if;
            set => _if = Adopt(value);
        }

        [SyntaxOptional]
        // TODO: consider renaming to 'TypeName' since it's not the resolved type reference
        public LiteralExpression? Type {
            get => _type;
            set => _type = Adopt(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope;
            set => _scope = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? Allow {
            get => _allow;
            set => _allow = Adopt(value);
        }

        [SyntaxOptional]
        public AExpression? Value {
            get => _value;
            set => _value = Adopt(value);
        }

        // TODO: allow conditional dependencies
        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> DependsOn {
            get => _dependsOn;
            set => _dependsOn = Adopt(value);
        }

        [SyntaxOptional]
        public ObjectExpression Properties {
            get => _properties;
            set => _properties = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? DefaultAttribute {
            get => _defaultAttribute;
            set => _defaultAttribute = Adopt(value);
        }

        [SyntaxOptional]
        public ListExpression Pragmas {
            get => _pragmas;
            set => _pragmas = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public LiteralExpression? DeletionPolicy {
            get => _deletionPolicy;
            set => _deletionPolicy = Adopt(value);
        }

        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasSecretType => Type!.Value == "Secret";
        public string? IfConditionName => ((ConditionReferenceExpression?)If)?.ReferenceName!.Value;

        //--- IResourceDeclaration Members ---
        LiteralExpression? IResourceDeclaration.ResourceTypeName => Type;
        bool IResourceDeclaration.HasInitialization => Value == null;
        bool IResourceDeclaration.HasPropertiesValidation => !HasPragma("no-type-validation");
        ObjectExpression IResourceDeclaration.Properties => Properties;
        AExpression? IResourceDeclaration.Condition => If;
    }
}