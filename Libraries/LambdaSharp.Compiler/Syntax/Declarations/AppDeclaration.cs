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
using LambdaSharp.Compiler.Syntax.EventSources;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("App")]
    public sealed class AppDeclaration : AItemDeclaration {

        //--- Types ---
        public sealed class ApiDeclaration : ASyntaxNode {

            //--- Fields ---
            private AExpression? _rootPath;
            private AExpression? _corsOrigin;
            private AExpression? _burstLimit;
            private AExpression? _rateLimit;

            //--- Properties ---

            [SyntaxOptional]
            public AExpression? RootPath {
                get => _rootPath;
                set => _rootPath = Adopt(value);
            }

            [SyntaxOptional]
            public AExpression? CorsOrigin {
                get => _corsOrigin;
                set => _corsOrigin = Adopt(value);
            }

            [SyntaxOptional]
            public AExpression? BurstLimit {
                get => _burstLimit;
                set => _burstLimit = Adopt(value);
            }

            [SyntaxOptional]
            public AExpression? RateLimit {
                get => _rateLimit;
                set => _rateLimit = Adopt(value);
            }
        }

        public sealed class BucketDeclaration : ASyntaxNode {

            //--- Fields ---
            private AExpression? _cloudFrontOriginAccessIdentity;
            private AExpression? _contentEncoding;

            //--- Properties ---

            [SyntaxOptional]
            public AExpression? CloudFrontOriginAccessIdentity {
                get => _cloudFrontOriginAccessIdentity;
                set => _cloudFrontOriginAccessIdentity = Adopt(value);
            }

            [SyntaxOptional]
            public AExpression? ContentEncoding {
                get => _contentEncoding;
                set => _contentEncoding = Adopt(value);
            }
        }

        public sealed class ClientDeclaration : ASyntaxNode {

            //--- Fields ---
            private AExpression? _apiUrl;

            //--- Properties ---

            [SyntaxOptional]
            public AExpression? ApiUrl {
                get => _apiUrl;
                set => _apiUrl = Adopt(value);
            }
        }

        //--- Fields ---
        private LiteralExpression? _project;
        private AExpression? _logRetentionInDays;
        private ListExpression _pragmas;
        private ApiDeclaration? _api;
        private BucketDeclaration? _bucket;
        private ClientDeclaration? _client;
        private ObjectExpression? _appSettings;
        private SyntaxNodeCollection<AEventSourceDeclaration> _sources;

        //--- Constructors ---
        public AppDeclaration(LiteralExpression itemName) : base(itemName) {
            _pragmas = Adopt(new ListExpression());
            _sources = Adopt(new SyntaxNodeCollection<AEventSourceDeclaration>());
        }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Project {
            get => _project;
            set => _project = Adopt(value);
        }

        [SyntaxOptional]
        public AExpression? LogRetentionInDays {
            get => _logRetentionInDays;
            set => _logRetentionInDays = Adopt(value);
        }

        [SyntaxOptional]
        public ListExpression Pragmas {
            get => _pragmas;
            set => _pragmas = Adopt(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public ApiDeclaration? Api {
            get => _api;
            set => _api = Adopt(value);
        }

        [SyntaxOptional]
        public BucketDeclaration? Bucket {
            get => _bucket;
            set => _bucket = Adopt(value);
        }

        [SyntaxOptional]
        public ClientDeclaration? Client {
            get => _client;
            set => _client = Adopt(value);
        }

        [SyntaxOptional]
        public ObjectExpression? AppSettings {
            get => _appSettings;
            set => _appSettings = Adopt(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<AEventSourceDeclaration> Sources {
            get => _sources;
            set => _sources = Adopt(value);
        }

        //--- Methods ---
        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasAppRegistration => !HasPragma("no-registration");
    }
}