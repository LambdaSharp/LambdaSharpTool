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

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class AItemDeclaration : ADeclaration {

        //--- Types ---

        // NOTE (2019-11-01, bjorg): this struct only exists to make it clear the 'AddDeclaration()' method should never be called directly!
        public struct DoNotCallThisDirectly { }

        //--- Fields ---
        private List<AItemDeclaration>? _internalDeclarations;
        private string? _fullName;
        private string? _logicalId;

        //--- Abstract Properties ---
        public abstract string? LocalName { get; }

        //--- Properties ---
        public string FullName {
            get => _fullName ?? throw new ArgumentNullException("value not set");
            set => _fullName = value ?? throw new NullValueException(nameof(FullName));
        }

        public string LogicalId {
            get => _logicalId ?? throw new ArgumentNullException("value not set");
            set => _logicalId = value ?? throw new NullValueException(nameof(LogicalId));
        }

        public LiteralExpression? Description { get; set; }
        public bool DiscardIfNotReachable { get; set; }
        public IEnumerable<ADeclaration> Declarations => _internalDeclarations ?? Enumerable.Empty<ADeclaration>();

        /// <summary>
        /// CFN expression to use when referencing the declaration. It could be a simple reference, a conditional, or an attribute, etc.
        /// </summary>
        public AExpression? ReferenceExpression { get; set; }

        /// <summary>
        /// List of declarations on which this declaration depends on.
        /// </summary>
        /// <param name="ReferenceName"></param>
        /// <param name="Conditions"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public List<(string ReferenceName, IEnumerable<AExpression> Conditions, AExpression Expression)> Dependencies { get; set; } = new List<(string, IEnumerable<AExpression>, AExpression)>();

        /// <summary>
        /// List of declarations that depend on this declaration.
        /// </summary>
        /// <typeparam name="ASyntaxNode"></typeparam>
        /// <returns></returns>
        public List<AExpression> ReverseDependencies { get; set; } = new List<AExpression>();

        //--- Methods ---
        public void AddDeclaration(AItemDeclaration declaration, DoNotCallThisDirectly _) {
            if(_internalDeclarations == null) {
                _internalDeclarations = new List<AItemDeclaration>();
            }
            _internalDeclarations.Add(declaration);
        }
    }

    public interface IScopedDeclaration {

        //--- Properties ---
        string FullName { get; }
        AExpression? Scope { get; }
        IEnumerable<string>? ScopeValues { get; }
        bool HasSecretType { get; }
        AExpression? ReferenceExpression { get; }
    }

    public interface IResourceDeclaration {

        //--- Properties ---
        string FullName { get; }
        string? CloudFormationType { get; }
    }

    public interface IConditionalResourceDeclaration : IResourceDeclaration {

        //--- Properties ---
        AExpression? If { get; }
        string? IfConditionName { get; }
    }

    public class ParameterDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Parameter { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Section { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Label { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Type { get; set; }

        [SyntaxOptional]
        public AExpression? Scope { get; set; }

        [SyntaxOptional]
        public LiteralExpression? NoEcho { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Default { get; set; }

        [SyntaxOptional]
        public LiteralExpression? ConstraintDescription { get; set; }

        [SyntaxOptional]
        public LiteralExpression? AllowedPattern { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> AllowedValues { get; set; } = new List<LiteralExpression>();

        [SyntaxOptional]
        public LiteralExpression? MaxLength { get; set; }

        [SyntaxOptional]
        public LiteralExpression? MaxValue { get; set; }

        [SyntaxOptional]
        public LiteralExpression? MinLength { get; set; }

        [SyntaxOptional]
        public LiteralExpression? MinValue { get; set; }

        [SyntaxOptional]
        public AExpression? Allow { get; set; }

        [SyntaxOptional]
        public ObjectExpression? Properties { get; set; }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext { get; set; }

        [SyntaxOptional]
        public ListExpression Pragmas { get; set; } = new ListExpression();

        public override string LocalName => Parameter!.Value;
        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasSecretType => Type!.Value == "Secret";
        public IEnumerable<string> ScopeValues => ((ListExpression)Scope!).Items.Cast<LiteralExpression>().Select(item => item.Value).ToList();

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Parameter?.Visit(this, visitor);
            Section?.Visit(this, visitor);
            Label?.Visit(this, visitor);
            Type?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            NoEcho?.Visit(this, visitor);
            Default?.Visit(this, visitor);
            ConstraintDescription?.Visit(this, visitor);
            AllowedPattern?.Visit(this, visitor);
            AllowedValues?.Visit(this, visitor);
            MaxLength?.Visit(this, visitor);
            MaxValue?.Visit(this, visitor);
            MinLength?.Visit(this, visitor);
            MinValue?.Visit(this, visitor);
            Allow?.Visit(this, visitor);
            Properties?.Visit(this, visitor);
            EncryptionContext?.Visit(this, visitor);
            Pragmas?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class PseudoParameterDeclaration : AItemDeclaration {

        //--- Properties ---
        public LiteralExpression? PseudoParameter { get; set; }
        public override string? LocalName => PseudoParameter?.Value;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) { }
    }

    public class ImportDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Import { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Type { get; set; }

        [SyntaxOptional]
        public AExpression? Scope { get; set; }

        [SyntaxOptional]
        public AExpression? Allow { get; set; }

        [SyntaxRequired]
        public LiteralExpression? Module { get; set; }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext { get; set; }

        public override string? LocalName => Import?.Value;
        public IEnumerable<string>? ScopeValues => ((ListExpression?)Scope)?.Items.Cast<LiteralExpression>().Select(item => item.Value).ToList();
        public bool HasSecretType => Type!.Value == "Secret";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Import?.Visit(this, visitor);
            Type?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Allow?.Visit(this, visitor);
            Module?.Visit(this, visitor);
            EncryptionContext?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class VariableDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Variable { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Type { get; set; }

        [SyntaxOptional]
        public AExpression? Scope { get; set; }

        [SyntaxRequired]
        public AExpression? Value { get; set; }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext { get; set; }

        public override string? LocalName => Variable?.Value;
        public IEnumerable<string>? ScopeValues => ((ListExpression?)Scope)?.Items.Cast<LiteralExpression>().Select(item => item.Value).ToList();
        public bool HasSecretType => Type!.Value == "Secret";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Variable?.Visit(this, visitor);
            Type?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Value?.Visit(this, visitor);
            EncryptionContext?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class GroupDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Group { get; set; }

        [SyntaxRequired]
        public List<AItemDeclaration> Items { get; set; } = new List<AItemDeclaration>();

        public override string? LocalName => Group?.Value;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Group?.Visit(this, visitor);
            Items?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ConditionDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Condition { get; set; }

        [SyntaxRequired]
        public AExpression? Value { get; set; }

        public override string? LocalName => Condition?.Value;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Condition?.Visit(this, visitor);
            Value?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ResourceDeclaration : AItemDeclaration, IScopedDeclaration, IConditionalResourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Resource { get; set; }

        [SyntaxOptional]
        public AExpression? If { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Type { get; set; }

        [SyntaxOptional]
        public AExpression? Scope { get; set; }

        [SyntaxOptional]
        public AExpression? Allow { get; set; }

        [SyntaxOptional]
        public AExpression? Value { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> DependsOn { get; set; } = new List<LiteralExpression>();

        [SyntaxOptional]
        public ObjectExpression Properties { get; set; } = new ObjectExpression();

        [SyntaxOptional]
        public LiteralExpression? DefaultAttribute { get; set; }

        [SyntaxOptional]
        public ListExpression Pragmas { get; set; } = new ListExpression();

        public override string? LocalName => Resource?.Value;
        public string? CloudFormationType => (Value == null) ? Type!.Value : null;
        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public IEnumerable<string>? ScopeValues => ((ListExpression?)Scope)?.Items.Cast<LiteralExpression>().Select(item => item.Value).ToList();
        public bool HasSecretType => Type!.Value == "Secret";
        public string? IfConditionName => ((ConditionExpression?)If)?.ReferenceName!.Value;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Resource?.Visit(this, visitor);
            If?.Visit(this, visitor);
            Type?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Allow?.Visit(this, visitor);
            Value?.Visit(this, visitor);
            DependsOn?.Visit(this, visitor);
            Properties?.Visit(this, visitor);
            DefaultAttribute?.Visit(this, visitor);
            Pragmas?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class NestedModuleDeclaration : AItemDeclaration, IResourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Nested { get; set; }

        [SyntaxRequired]
        public LiteralExpression? Module { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> DependsOn { get; set; } = new List<LiteralExpression>();

        [SyntaxOptional]
        public ObjectExpression? Parameters { get; set; }

        public override string? LocalName => Nested?.Value;
        public string CloudFormationType => "AWS::CloudFormation::Stack";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Nested?.Visit(this, visitor);
            Module?.Visit(this, visitor);
            DependsOn?.Visit(this, visitor);
            Parameters?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class PackageDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Properties --

        [SyntaxKeyword]
        public LiteralExpression? Package { get; set; }

        [SyntaxOptional]
        public AExpression? Scope { get; set; }

        [SyntaxRequired]
        public LiteralExpression? Files { get; set; }

        public override string? LocalName => Package?.Value;

        public List<KeyValuePair<string, string>> ResolvedFiles { get; set; } = new List<KeyValuePair<string, string>>();
        public IEnumerable<string>? ScopeValues => ((ListExpression?)Scope)?.Items.Cast<LiteralExpression>().Select(item => item.Value).ToList();
        public bool HasSecretType => false;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Package?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Files?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class FunctionDeclaration : AItemDeclaration, IScopedDeclaration, IConditionalResourceDeclaration {

        //--- Types ---
        public class VpcExpression : ASyntaxNode {

            //--- Properties ---

            [SyntaxRequired]
            public AExpression? SecurityGroupIds { get;set; }

            [SyntaxRequired]
            public AExpression? SubnetIds { get;set; }

            //--- Methods ---
            public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
                visitor.VisitStart(parent, this);
                SecurityGroupIds?.Visit(this, visitor);
                SubnetIds?.Visit(this, visitor);
                visitor.VisitEnd(parent, this);
            }
        }

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Function { get; set; }

        [SyntaxOptional]
        public AExpression? Scope { get; set; }

        [SyntaxOptional]
        public AExpression? If { get; set; }

        [SyntaxRequired]
        public AExpression? Memory { get; set; }

        [SyntaxRequired]
        public AExpression? Timeout { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Project { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Runtime { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Language { get; set; }

        [SyntaxOptional]
        public LiteralExpression? Handler { get; set; }

        [SyntaxOptional]
        public VpcExpression? Vpc { get; set; }

        [SyntaxOptional]
        public ObjectExpression Environment { get; set; } = new ObjectExpression();

        [SyntaxOptional]
        public ObjectExpression Properties { get; set; } = new ObjectExpression();

        [SyntaxOptional]
        public List<AEventSourceDeclaration> Sources { get; set; } = new List<AEventSourceDeclaration>();

        [SyntaxOptional]
        public ListExpression Pragmas { get; set; } = new ListExpression();

        public override string? LocalName => Function?.Value;
        public string CloudFormationType => "AWS::Lambda::Function";

        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasDeadLetterQueue => !HasPragma("no-dead-letter-queue");
        public bool HasAssemblyValidation => !HasPragma("no-assembly-validation");
        public bool HasHandlerValidation => !HasPragma("no-handler-validation");
        public bool HasWildcardScopedVariables => !HasPragma("no-wildcard-scoped-variables");
        public bool HasFunctionRegistration => !HasPragma("no-function-registration");
        public IEnumerable<string>? ScopeValues => ((ListExpression?)Scope)?.Items.Cast<LiteralExpression>().Select(item => item.Value).ToList();
        public bool HasSecretType => false;
        public string? IfConditionName => ((ConditionExpression?)If)?.ReferenceName!.Value;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Function?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            If?.Visit(this, visitor);
            Memory?.Visit(this, visitor);
            Timeout?.Visit(this, visitor);
            Project?.Visit(this, visitor);
            Runtime?.Visit(this, visitor);
            Language?.Visit(this, visitor);
            Handler?.Visit(this, visitor);
            Vpc?.Visit(this, visitor);
            Environment?.Visit(this, visitor);
            Properties?.Visit(this, visitor);
            Sources?.Visit(this, visitor);
            Pragmas?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class MappingDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Mapping { get; set; }

        [SyntaxRequired]
        public ObjectExpression? Value { get; set; }

        public override string LocalName => Mapping!.Value;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Mapping?.Visit(this, visitor);
            Value?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ResourceTypeDeclaration : AItemDeclaration {

        //--- Types ---
        public class PropertyTypeExpression : ASyntaxNode {

            //--- Properties ---

            [SyntaxKeyword]
            public LiteralExpression? Name { get; set; }

            [SyntaxRequired]
            public LiteralExpression? Type { get; set; }

            [SyntaxOptional]
            public LiteralExpression? Required { get; set; }

            //--- Methods ---
            public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
                visitor.VisitStart(parent, this);
                Name?.Visit(this, visitor);
                Type?.Visit(this, visitor);
                Required?.Visit(this, visitor);
                visitor.VisitEnd(parent, this);
            }
        }

        public class AttributeTypeExpression : ASyntaxNode {

            //--- Properties ---

            [SyntaxKeyword]
            public LiteralExpression? Name { get; set; }

            [SyntaxRequired]
            public LiteralExpression? Type { get; set; }

            //--- Methods ---
            public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
                visitor.VisitStart(parent, this);
                Name?.Visit(this, visitor);
                Type?.Visit(this, visitor);
                visitor.VisitEnd(parent, this);
            }
        }
        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? ResourceType { get; set; }

        [SyntaxRequired]
        public LiteralExpression? Handler { get; set; }

        [SyntaxOptional]
        public List<PropertyTypeExpression> Properties { get; set; } = new List<PropertyTypeExpression>();

        [SyntaxOptional]
        public List<AttributeTypeExpression> Attributes { get; set; } = new List<AttributeTypeExpression>();

        public override string LocalName => ResourceType!.Value;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ResourceType?.Visit(this, visitor);
            Handler?.Visit(this, visitor);
            Properties?.Visit(this, visitor);
            Attributes?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class MacroDeclaration : AItemDeclaration, IResourceDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression? Macro { get; set; }

        [SyntaxRequired]
        public LiteralExpression? Handler { get; set; }

        public override string LocalName => Macro!.Value;
        public string CloudFormationType => "AWS::CloudFormation::Macro";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Macro?.Visit(this, visitor);
            Handler?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }
}