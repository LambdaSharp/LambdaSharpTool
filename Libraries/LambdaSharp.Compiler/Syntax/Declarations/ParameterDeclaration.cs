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
using System.Linq;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Parameter")]
    public sealed class ParameterDeclaration :
        AItemDeclaration,
        IScopedDeclaration,
        IResourceDeclaration
    {

        //--- Fields ---
        private LiteralExpression? _section;
        private LiteralExpression? _label;
        private LiteralExpression? _type;
        private SyntaxNodeCollection<LiteralExpression> _scope;
        private LiteralExpression? _noEcho;
        private LiteralExpression? _default;
        private LiteralExpression? _constraintDescription;
        private LiteralExpression? _allowedPattern;
        private SyntaxNodeCollection<LiteralExpression> _allowedValues;
        private LiteralExpression? _maxLength;
        private LiteralExpression? _maxValue;
        private LiteralExpression? _minLength;
        private LiteralExpression? _minValue;
        private SyntaxNodeCollection<LiteralExpression>? _allow;
        private ObjectExpression? _properties;
        private ObjectExpression? _encryptionContext;
        private LiteralExpression? _defaultAttribute;
        private ListExpression _pragmas;
        private LiteralExpression? _deletionPolicy;

        //--- Constructors ---
        public ParameterDeclaration(LiteralExpression itemName) : base(itemName) {
            _scope = Adopt(new SyntaxNodeCollection<LiteralExpression>());
            _allowedValues = Adopt(new SyntaxNodeCollection<LiteralExpression>());
            _pragmas = Adopt(new ListExpression());
        }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Section {
            get => _section;
            set => _section = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Label {
            get => _label;
            set => _label = Adopt(value);
        }

        [SyntaxOptional]
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
        public LiteralExpression? NoEcho {
            get => _noEcho;
            set => _noEcho = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Default {
            get => _default;
            set => _default = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? ConstraintDescription {
            get => _constraintDescription;
            set => _constraintDescription = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? AllowedPattern {
            get => _allowedPattern;
            set => _allowedPattern = Adopt(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> AllowedValues {
            get => _allowedValues;
            set => _allowedValues = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? MaxLength {
            get => _maxLength;
            set => _maxLength = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? MaxValue {
            get => _maxValue;
            set => _maxValue = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? MinLength {
            get => _minLength;
            set => _minLength = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? MinValue {
            get => _minValue;
            set => _minValue = Adopt(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? Allow {
            get => _allow;
            set => _allow = Adopt(value);
        }

        [SyntaxOptional]
        public ObjectExpression? Properties {
            get => _properties;
            set => _properties = Adopt(value);
        }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext {
            get => _encryptionContext;
            set => _encryptionContext = Adopt(value);
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

        //--- IResourceDeclaration Members ---
        LiteralExpression? IResourceDeclaration.ResourceTypeName => Type;
        bool IResourceDeclaration.HasInitialization => Properties != null;
        bool IResourceDeclaration.HasPropertiesValidation => !HasPragma("no-type-validation");
        ObjectExpression IResourceDeclaration.Properties => Properties ?? throw new InvalidOperationException();

        // TODO: this should probably not always be null
        AExpression? IResourceDeclaration.Condition => null;
    }
}