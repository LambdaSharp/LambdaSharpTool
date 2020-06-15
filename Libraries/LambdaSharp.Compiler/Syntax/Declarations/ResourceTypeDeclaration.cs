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
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("ResourceType")]
    public sealed class ResourceTypeDeclaration : AItemDeclaration {

        //--- Types ---
        public class PropertyTypeExpression : ASyntaxNode {

            //--- Fields ---
            private LiteralExpression? _name;
            private LiteralExpression? _description;
            private LiteralExpression? _type;
            private LiteralExpression? _required;

            //--- Properties ---

            [SyntaxRequired]
            public LiteralExpression Name {
                get => _name ?? throw new InvalidOperationException();
                set => _name = SetParent(value ?? throw new ArgumentNullException());
            }

            [SyntaxOptional]
            public LiteralExpression? Description {
                get => _description;
                set => _description = SetParent(value);
            }

            [SyntaxOptional]
            public LiteralExpression? Type {
                get => _type;
                set => _type = SetParent(value);
            }

            [SyntaxOptional]
            public LiteralExpression? Required {
                get => _required;
                set => _required = SetParent(value);
            }
        }

        public class AttributeTypeExpression : ASyntaxNode {

            //--- Fields ---
            private LiteralExpression? _name;
            private LiteralExpression? _description;
            private LiteralExpression? _type;

            //--- Properties ---

            [SyntaxRequired]
            public LiteralExpression Name {
                get => _name ?? throw new InvalidOperationException();
                set => _name = SetParent(value ?? throw new ArgumentNullException());
            }

            [SyntaxOptional]
            public LiteralExpression? Description {
                get => _description;
                set => _description = SetParent(value);
            }

            [SyntaxOptional]
            public LiteralExpression? Type {
                get => _type;
                set => _type = SetParent(value);
            }
        }

        //--- Fields ---
        private AExpression? _handler;
        private LiteralExpression? _documentation;
        private SyntaxNodeCollection<PropertyTypeExpression> _properties;
        private SyntaxNodeCollection<AttributeTypeExpression> _attributes;

        //--- Constructors ---
        public ResourceTypeDeclaration(LiteralExpression itemName) : base(itemName) {
            _properties = SetParent(new SyntaxNodeCollection<PropertyTypeExpression>());
            _attributes = SetParent(new SyntaxNodeCollection<AttributeTypeExpression>());
        }

        //--- Properties ---

        [SyntaxRequired]
        public AExpression? Handler {
            get => _handler;
            set => _handler = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Documentation {
            get => _documentation;
            set => _documentation = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<PropertyTypeExpression> Properties {
            get => _properties;
            set => _properties = value;
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<AttributeTypeExpression> Attributes {
            get => _attributes;
            set => _attributes = value;
        }
    }
}