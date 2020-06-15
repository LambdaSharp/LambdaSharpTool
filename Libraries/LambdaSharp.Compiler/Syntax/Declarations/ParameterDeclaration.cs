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
        private ListExpression _pragmas;
        private LiteralExpression? _import;

        //--- Constructors ---
        public ParameterDeclaration(LiteralExpression itemName) : base(itemName) {
            _scope = SetParent(new SyntaxNodeCollection<LiteralExpression>());
            _allowedValues = SetParent(new SyntaxNodeCollection<LiteralExpression>());
            _pragmas = SetParent(new ListExpression());
        }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Section {
            get => _section;
            set => _section = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Label {
            get => _label;
            set => _label = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Type {
            get => _type;
            set => _type = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope;
            set => _scope = SetParent(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public LiteralExpression? NoEcho {
            get => _noEcho;
            set => _noEcho = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Default {
            get => _default;
            set => _default = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? ConstraintDescription {
            get => _constraintDescription;
            set => _constraintDescription = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? AllowedPattern {
            get => _allowedPattern;
            set => _allowedPattern = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> AllowedValues {
            get => _allowedValues;
            set => _allowedValues = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? MaxLength {
            get => _maxLength;
            set => _maxLength = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? MaxValue {
            get => _maxValue;
            set => _maxValue = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? MinLength {
            get => _minLength;
            set => _minLength = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? MinValue {
            get => _minValue;
            set => _minValue = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? Allow {
            get => _allow;
            set => _allow = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression? Properties {
            get => _properties;
            set => _properties = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext {
            get => _encryptionContext;
            set => _encryptionContext = SetParent(value);
        }

        [SyntaxOptional]
        public ListExpression Pragmas {
            get => _pragmas;
            set => _pragmas = SetParent(value);
        }

        [SyntaxHidden]
        public LiteralExpression? Import {
            get => _import;
            set => _import = SetParent(value);
        }

        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasSecretType => Type!.Value == "Secret";

        //--- IResourceDeclaration Members ---
        LiteralExpression? IResourceDeclaration.ResourceTypeName => Type;
        bool IResourceDeclaration.HasInitialization => Properties != null;
        bool IResourceDeclaration.HasPropertiesValidation => !HasPragma("no-type-validation");
        ObjectExpression IResourceDeclaration.Properties => Properties ?? throw new InvalidOperationException();
    }
}