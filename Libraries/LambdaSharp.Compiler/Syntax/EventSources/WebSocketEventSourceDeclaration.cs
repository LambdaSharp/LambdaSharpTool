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
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.EventSources {

    [SyntaxDeclarationKeyword("WebSocket")]
    public sealed class WebSocketEventSourceDeclaration : AEventSourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _operationName;
        private LiteralExpression? _apiKeyRequired;
        private LiteralExpression? _authorizationType;
        private SyntaxNodeCollection<LiteralExpression>? _authorizationScopes;
        private AExpression? _authorizerId;
        private LiteralExpression? _invoke;

        //--- Constructors ---
        public WebSocketEventSourceDeclaration(LiteralExpression eventSource) => EventSource = SetParent(eventSource ?? throw new ArgumentNullException(nameof(eventSource)));

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? OperationName {
            get => _operationName;
            set => _operationName = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? ApiKeyRequired {
            get => _apiKeyRequired;
            set => _apiKeyRequired = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? AuthorizationType {
            get => _authorizationType;
            set => _authorizationType = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression>? AuthorizationScopes {
            get => _authorizationScopes;
            set => _authorizationScopes = SetParent(value);
        }

        [SyntaxOptional]
        public AExpression? AuthorizerId {
            get => _authorizerId;
            set => _authorizerId = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Invoke {
            get => _invoke;
            set => _invoke = SetParent(value);
        }

        public LiteralExpression EventSource { get; }
    }
}