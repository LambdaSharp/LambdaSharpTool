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
using System.Linq;
using LambdaSharp.Compiler.Exceptions;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    [SyntaxDeclarationKeyword("Function")]
    public sealed class FunctionDeclaration : AItemDeclaration, IScopedDeclaration, IConditionalResourceDeclaration {

        //--- Types ---
        public class VpcExpression : ASyntaxNode {

            //--- Fields ---
            private AExpression? _securityGroupIds;
            private AExpression? _subnetIds;

            //--- Properties ---

            [SyntaxRequired]
            public AExpression? SecurityGroupIds {
                get => _securityGroupIds;
                set => _securityGroupIds = SetParent(value);
            }

            [SyntaxRequired]
            public AExpression? SubnetIds {
                get => _subnetIds;
                set => _subnetIds = SetParent(value);
            }

            //--- Methods ---
            public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
                if(!visitor.VisitStart(this)) {
                return this;
            }
                SecurityGroupIds = SecurityGroupIds?.Visit(visitor);
                SubnetIds = SubnetIds?.Visit(visitor);
                return visitor.VisitEnd(this);
            }

            public override void InspectNode(Action<ASyntaxNode> inspector) {
                inspector(this);
                SecurityGroupIds?.InspectNode(inspector);
                SubnetIds?.InspectNode(inspector);
            }
        }

        //--- Fields ---
        private SyntaxNodeCollection<LiteralExpression> _scope;
        private AExpression? _if;
        private AExpression? _memory;
        private AExpression? _timeout;
        private LiteralExpression? _project;
        private LiteralExpression? _runtime;
        private LiteralExpression? _language;
        private LiteralExpression? _handler;
        private VpcExpression? _vpc;
        private ObjectExpression _environment;
        private ObjectExpression _properties;
        private SyntaxNodeCollection<AEventSourceDeclaration> _sources;
        private ListExpression _pragmas;

        //--- Constructors ---
        public FunctionDeclaration(LiteralExpression itemName) : base(itemName) {
            _scope = SetParent(new SyntaxNodeCollection<LiteralExpression>());
            _environment = SetParent(new ObjectExpression());
            _properties = SetParent(new ObjectExpression());
            _sources = _sources = new SyntaxNodeCollection<AEventSourceDeclaration>();
            _pragmas = SetParent(new ListExpression());
        }

        //--- Properties ---

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope;
            set => _scope = SetParent(value ?? throw new ArgumentNullException());
        }

        [SyntaxOptional]
        public AExpression? If {
            get => _if;
            set => _if = SetParent(value);
        }

        [SyntaxRequired]
        public AExpression? Memory {
            get => _memory;
            set => _memory = SetParent(value);
        }

        [SyntaxRequired]
        public AExpression? Timeout {
            get => _timeout;
            set => _timeout = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Project {
            get => _project;
            set => _project = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Runtime {
            get => _runtime;
            set => _runtime = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Language {
            get => _language;
            set => _language = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Handler {
            get => _handler;
            set => _handler = SetParent(value);
        }

        // TODO (2020-01-30, bjorg): this notation is deprecated, use `VpcConfig` in `Properties` instead
        [SyntaxOptional]
        public VpcExpression? Vpc {
            get => _vpc;
            set => _vpc = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression Environment {
            get => _environment;
            set => _environment = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression Properties {
            get => _properties;
            set => _properties = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<AEventSourceDeclaration> Sources {
            get => _sources;
            set => _sources = SetParent(value);
        }

        [SyntaxOptional]
        public ListExpression Pragmas {
            get => _pragmas;
            set => _pragmas = SetParent(value);
        }

        public string CloudFormationType => "AWS::Lambda::Function";

        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasDeadLetterQueue => !HasPragma("no-dead-letter-queue");
        public bool HasAssemblyValidation => !HasPragma("no-assembly-validation");
        public bool HasHandlerValidation => !HasPragma("no-handler-validation");
        public bool HasWildcardScopedVariables => !HasPragma("no-wildcard-scoped-variables");
        public bool HasFunctionRegistration => !HasPragma("no-function-registration");
        public bool HasSecretType => false;
        public string? IfConditionName => ((ConditionExpression?)If)?.ReferenceName!.Value;
        public LiteralExpression? Type => Fn.Literal("AWS::Lambda::Function");

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ISyntaxVisitor visitor) {
            if(!visitor.VisitStart(this)) {
                return this;
            }
            AssertIsSame(ItemName, ItemName.Visit(visitor));
            Scope = Scope.Visit(visitor);
            If = If?.Visit(visitor);
            Memory = Memory?.Visit(visitor);
            Timeout = Timeout?.Visit(visitor);
            Project = Project?.Visit(visitor);
            Runtime = Runtime?.Visit(visitor);
            Language = Language?.Visit(visitor);
            Handler = Handler?.Visit(visitor);
            Vpc = Vpc?.Visit(visitor);
            Environment = Environment.Visit(visitor) ?? throw new NullValueException();
            Properties = Properties.Visit(visitor) ?? throw new NullValueException();
            Sources = Sources.Visit(visitor);
            Pragmas = Pragmas.Visit(visitor) ?? throw new NullValueException();
            Declarations = Declarations.Visit(visitor);
            return visitor.VisitEnd(this);
        }

        public override void InspectNode(Action<ASyntaxNode> inspector) {
            inspector(this);
            ItemName.InspectNode(inspector);
            Scope.InspectNode(inspector);
            If?.InspectNode(inspector);
            Memory?.InspectNode(inspector);
            Timeout?.InspectNode(inspector);
            Project?.InspectNode(inspector);
            Runtime?.InspectNode(inspector);
            Language?.InspectNode(inspector);
            Handler?.InspectNode(inspector);
            Vpc?.InspectNode(inspector);
            Environment.InspectNode(inspector);
            Properties.InspectNode(inspector);
            Sources.InspectNode(inspector);
            Pragmas.InspectNode(inspector);
            Declarations.InspectNode(inspector);
        }
    }
}