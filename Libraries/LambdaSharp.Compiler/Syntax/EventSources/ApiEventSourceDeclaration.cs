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
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.EventSources {

    [SyntaxDeclarationKeyword("Api")]
    public sealed class ApiEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _integration;
        private LiteralExpression? _operationName;
        private AExpression? _apiKeyRequired;
        private LiteralExpression? _authorizationType;
        private SyntaxNodeCollection<LiteralExpression>? _authorizationScopes;
        private AExpression? _authorizerId;
        private LiteralExpression? _invoke;

        //--- Types ---
        public enum IntegrationType {
            Unsupported,
            RequestResponse,
            SlackCommand
        }

        //--- Constructors ---
        public ApiEventSourceDeclaration(LiteralExpression eventSource) => EventSource = Adopt(eventSource ?? throw new ArgumentNullException(nameof(eventSource)));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Integration {
            get => _integration;
            set => _integration = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? OperationName {
            get => _operationName;
            set => _operationName = Adopt(value);
        }

        [SyntaxOptional]
        public AExpression? ApiKeyRequired {
            get => _apiKeyRequired;
            set => _apiKeyRequired = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? AuthorizationType {
            get => _authorizationType;
            set => _authorizationType = Adopt(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? AuthorizationScopes {
            get => _authorizationScopes;
            set => _authorizationScopes = Adopt(value);
        }

        [SyntaxOptional]
        public AExpression? AuthorizerId {
            get => _authorizerId;
            set => _authorizerId = Adopt(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Invoke {
            get => _invoke;
            set => _invoke = Adopt(value);
        }

        public LiteralExpression EventSource { get; }
        public string? ApiMethod { get; set; }
        public string[]? ApiPath { get; set; }
        public IntegrationType ApiIntegrationType { get; set; }
    }
}