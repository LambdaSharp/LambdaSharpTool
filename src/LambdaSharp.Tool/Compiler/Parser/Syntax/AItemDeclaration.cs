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
        public class DependencyRecord {

            //--- Constructors ---
            public DependencyRecord(AItemDeclaration referencedDeclaration, IEnumerable<AExpression> conditions, AExpression expression) {
                ReferencedDeclaration = referencedDeclaration ?? throw new ArgumentNullException(nameof(referencedDeclaration));
                Conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
                Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            }

            //--- Properties ---
            public AItemDeclaration ReferencedDeclaration { get; }
            public IEnumerable<AExpression> Conditions { get; }
            public AExpression Expression { get; }
        }

        //--- Fields ---
        private string? _logicalId;
        private LiteralExpression? _description;
        private readonly List<DependencyRecord> _dependencies = new List<DependencyRecord>();
        private readonly List<AExpression> _reverseDependencies = new List<AExpression>();

        //--- Constructors ---
        protected AItemDeclaration(LiteralExpression itemName) {
            ItemName = SetParent(itemName) ?? throw new ArgumentNullException(nameof(itemName));
            Declarations = SetParent(new SyntaxNodeCollection<AItemDeclaration>());
        }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Description {
            get => _description;
            set => _description = SetParent(value);
        }

        public LiteralExpression ItemName { get; }
        public string FullName => (ParentItemDeclaration != null) ? $"{ParentItemDeclaration.FullName}::{ItemName.Value}" : ItemName.Value;

        public string LogicalId {
            get => _logicalId ?? throw new InvalidOperationException("value not set");
            set => _logicalId = value ?? throw new NullValueException(nameof(value));
        }

        public bool DiscardIfNotReachable { get; set; }
        public SyntaxNodeCollection<AItemDeclaration> Declarations { get; }

        /// <summary>
        /// CloudFormation expression to use when referencing the declaration. It could be a simple reference, a conditional, or an attribute, etc.
        /// </summary>
        public AExpression? ReferenceExpression { get; set; }

        /// <summary>
        /// List of declarations on which this declaration depends on.
        /// </summary>
        /// <param name="ReferenceName"></param>
        /// <param name="Conditions"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public IEnumerable<DependencyRecord> Dependencies => _dependencies;

        /// <summary>
        /// List of declarations that depend on this declaration.
        /// </summary>
        /// <typeparam name="ASyntaxNode"></typeparam>
        /// <returns></returns>
        public IEnumerable<AExpression> ReverseDependencies => _reverseDependencies;

        //--- Methods ---
        public void TrackDependency(AItemDeclaration referencedDeclaration, AExpression dependentExpression) {
            if(referencedDeclaration is null) {
                throw new ArgumentNullException(nameof(referencedDeclaration));
            }
            if(dependentExpression is null) {
                throw new ArgumentNullException(nameof(dependentExpression));
            }
            _dependencies.Add(new AItemDeclaration.DependencyRecord(referencedDeclaration, FindConditions(dependentExpression), dependentExpression));
            referencedDeclaration._reverseDependencies.Add(dependentExpression);

            // local functions
            IEnumerable<AExpression> FindConditions(ASyntaxNode node) {
                var conditions = new List<AExpression>();
                ASyntaxNode? previousParent = null;
                foreach(var parent in node.Parents) {

                    // check if parent is an !If expression
                    if(parent is IfFunctionExpression ifParent) {

                        // determine if reference came from IfTrue or IfFalse path
                        if(object.ReferenceEquals(ifParent.IfTrue, previousParent)) {
                            conditions.Add(ifParent.Condition);
                        } else if(object.ReferenceEquals(ifParent.IfFalse, previousParent)) {

                            // TODO: review this one more time

                            // for IfFalse, create a !Not intermediary node
                            conditions.Add(new NotConditionExpression {
                                SourceLocation = ifParent.Condition.SourceLocation,
                                Value = ifParent.Condition
                            });
                        } else {
                            throw new ShouldNeverHappenException();
                        }
                    }
                    previousParent = parent;
                }
                conditions.Reverse();
                return conditions;
            }
        }

        public void UntrackDependency(AExpression dependentExpression)
            => _reverseDependencies.RemoveAll(reverseDependency => reverseDependency == dependentExpression);

        public void UntrackAllDependencies() {

            // iterate over all dependencies for this declaration and remove itself from the reverse dependencies list
            foreach(var dependency in _dependencies) {

                // TODO: this is what it should be, but not all expressions have `ReferencedDeclaration`; introduce interface we can filter on
                // dependency.Expression.ReferencedDeclaration = null;
                dependency.ReferencedDeclaration
                    ._reverseDependencies
                    .RemoveAll(expression => expression.ParentItemDeclaration == this);
            }
            _dependencies.Clear();
        }
    }

    /// <summary>
    /// The <see cref="IScopedDeclaration"/> interface indicates a resources that
    /// can be scoped to a Lambda function environment.
    /// </summary>
    public interface IScopedDeclaration {

        //--- Properties ---
        string FullName { get; }
        SyntaxNodeCollection<LiteralExpression>? Scope { get; }
        IEnumerable<string>? ScopeValues => Scope.Select(item => item.Value).ToList();
        bool HasSecretType { get; }
        AExpression? ReferenceExpression { get; }
    }

    /// <summary>
    /// The <see cref="IResourceDeclaration"/> interface indicates a CloudFormation resource that
    /// is created by the template.
    /// </summary>
    public interface IResourceDeclaration {

        //--- Properties ---
        string FullName { get; }
        string? CloudFormationType { get; }
    }

    /// <summary>
    /// The <see cref="IConditionalResourceDeclaration"/> interface indicates a CloudFormation resource that
    /// can be conditionally created by the template.
    /// </summary>
    public interface IConditionalResourceDeclaration : IResourceDeclaration {

        //--- Properties ---
        AExpression? If { get; }
        string? IfConditionName { get; }
    }

    [SyntaxDeclarationKeyword("Parameter")]
    public class ParameterDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Fields ---
        private LiteralExpression? _section;
        private LiteralExpression? _label;
        private LiteralExpression? _type;
        private SyntaxNodeCollection<LiteralExpression>? _scope;
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

        //--- Constructors ---
        public ParameterDeclaration(LiteralExpression itemName) : base(itemName) {
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
            get => _scope ?? throw new InvalidOperationException();
            set => _scope = SetParent(value) ?? throw new ArgumentNullException();
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
        public SyntaxNodeCollection<LiteralExpression> Allow {
            get => _allow ?? throw new InvalidOperationException();
            set => _allow = SetParent(value) ?? throw new ArgumentNullException();
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

        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasSecretType => Type!.Value == "Secret";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
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

    /// <summary>
    /// The <see cref="PseudoParameterDeclaration"/> class is used to declare CloudFormation pseudo-parameters.
    /// This declaration type is only used internally and never parsed.
    /// </summary>
    public class PseudoParameterDeclaration : AItemDeclaration {

        //--- Fields ---
        private LiteralExpression? pseudoParameter;

        //--- Constructors ---
        public PseudoParameterDeclaration(LiteralExpression itemName) : base(itemName) { }

        //--- Properties ---
        public LiteralExpression? PseudoParameter {
            get => pseudoParameter;
            set => pseudoParameter = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) { }
    }

    [SyntaxDeclarationKeyword("Import")]
    public class ImportDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Fields ---
        private LiteralExpression? _type;
        private SyntaxNodeCollection<LiteralExpression>? _scope;
        private SyntaxNodeCollection<LiteralExpression>? _allow;
        private LiteralExpression? _module;
        private ObjectExpression? _encryptionContext;

        //--- Constructors ---
        public ImportDeclaration(LiteralExpression itemName) : base(itemName) { }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Type {
            get => _type;
            set => _type = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope ?? throw new InvalidOperationException();
            set => _scope = SetParent(value) ?? throw new ArgumentNullException();
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Allow {
            get => _allow ?? throw new InvalidOperationException();
            set => _allow = SetParent(value) ?? throw new ArgumentNullException();
        }

        [SyntaxRequired]
        public LiteralExpression? Module {
            get => _module;
            set => _module = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext {
            get => _encryptionContext;
            set => _encryptionContext = SetParent(value);
        }

        public bool HasSecretType => Type!.Value == "Secret";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Type?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Allow?.Visit(this, visitor);
            Module?.Visit(this, visitor);
            EncryptionContext?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Variable")]
    public class VariableDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Fields ---
        private LiteralExpression? type;
        private SyntaxNodeCollection<LiteralExpression>? _scope;
        private AExpression? _value;
        private ObjectExpression? _encryptionContext;

        //--- Constructors ---
        public VariableDeclaration(LiteralExpression itemName) : base(itemName) { }

        //--- Properties ---

        [SyntaxOptional]
        public LiteralExpression? Type {
            get => type;
            set => type = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope ?? throw new InvalidOperationException();
            set => _scope = SetParent(value) ?? throw new ArgumentNullException();
        }

        [SyntaxRequired]
        public AExpression? Value {
            get => _value;
            set => _value = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression? EncryptionContext {
            get => _encryptionContext;
            set => _encryptionContext = SetParent(value);
        }

        public bool HasSecretType => Type!.Value == "Secret";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Type?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Value?.Visit(this, visitor);
            EncryptionContext?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Group")]
    public class GroupDeclaration : AItemDeclaration {

        //--- Fields ---
        private SyntaxNodeCollection<AItemDeclaration> _items;

        //--- Constructors ---
        public GroupDeclaration(LiteralExpression itemName) : base(itemName) {
            _items = SetParent(new SyntaxNodeCollection<AItemDeclaration>());
        }

        //--- Properties ---

        [SyntaxRequired]
        public SyntaxNodeCollection<AItemDeclaration> Items {
            get => _items;
            set => _items = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Items?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Condition")]
    public class ConditionDeclaration : AItemDeclaration {

        //--- Fields ---
        private AExpression? _value;

        //--- Constructors ---
        public ConditionDeclaration(LiteralExpression itemName) : base(itemName) { }

        //--- Properties ---

        [SyntaxRequired]
        public AExpression? Value {
            get => _value;
            set => _value = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Value?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Resource")]
    public class ResourceDeclaration : AItemDeclaration, IScopedDeclaration, IConditionalResourceDeclaration {

        //--- Fields ---
        private AExpression? _if;
        private LiteralExpression? _type;
        private SyntaxNodeCollection<LiteralExpression>? _scope;
        private SyntaxNodeCollection<LiteralExpression>? _allow;
        private AExpression? _value;
        private SyntaxNodeCollection<LiteralExpression> _dependsOn;
        private ObjectExpression _properties;
        private LiteralExpression? _defaultAttribute;
        private ListExpression _pragmas;

        //--- Constructors ---
        public ResourceDeclaration(LiteralExpression itemName) : base(itemName) {
            _dependsOn = SetParent(new SyntaxNodeCollection<LiteralExpression>());
            _properties = SetParent(new ObjectExpression());
            _pragmas = SetParent(new ListExpression());
        }

        //--- Properties ---

        [SyntaxOptional]
        public AExpression? If {
            get => _if;
            set => _if = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? Type {
            get => _type;
            set => _type = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope ?? throw new InvalidOperationException();
            set => _scope = SetParent(value) ?? throw new ArgumentNullException();
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Allow {
            get => _allow ?? throw new InvalidOperationException();
            set => _allow = SetParent(value) ?? throw new ArgumentNullException();
        }

        [SyntaxOptional]
        public AExpression? Value {
            get => _value;
            set => _value = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> DependsOn {
            get => _dependsOn;
            set => _dependsOn = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression Properties {
            get => _properties;
            set => _properties = SetParent(value);
        }

        [SyntaxOptional]
        public LiteralExpression? DefaultAttribute {
            get => _defaultAttribute;
            set => _defaultAttribute = SetParent(value);
        }

        [SyntaxOptional]
        public ListExpression Pragmas {
            get => _pragmas;
            set => _pragmas = SetParent(value);
        }

        public string? CloudFormationType => (Value == null) ? Type!.Value : null;
        public bool HasPragma(string pragma) => Pragmas.Any(expression => (expression is LiteralExpression literalExpression) && (literalExpression.Value == pragma));
        public bool HasSecretType => Type!.Value == "Secret";
        public string? IfConditionName => ((ConditionExpression?)If)?.ReferenceName!.Value;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
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

    [SyntaxDeclarationKeyword("Nested")]
    public class NestedModuleDeclaration : AItemDeclaration, IResourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _module;
        private SyntaxNodeCollection<LiteralExpression> _dependsOn;
        private ObjectExpression? _parameters;

        //--- Constructors ---
        public NestedModuleDeclaration(LiteralExpression itemName) : base(itemName) {
            _dependsOn = SetParent(new SyntaxNodeCollection<LiteralExpression>());
        }

        //--- Properties ---

        [SyntaxRequired]
        public LiteralExpression? Module {
            get => _module;
            set => _module = SetParent(value);
        }

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> DependsOn {
            get => _dependsOn;
            set => _dependsOn = SetParent(value);
        }

        [SyntaxOptional]
        public ObjectExpression? Parameters {
            get => _parameters;
            set => _parameters = SetParent(value);
        }

        public string CloudFormationType => "AWS::CloudFormation::Stack";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Module?.Visit(this, visitor);
            DependsOn?.Visit(this, visitor);
            Parameters?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Package")]
    public class PackageDeclaration : AItemDeclaration, IScopedDeclaration {

        //--- Fields ---
        private SyntaxNodeCollection<LiteralExpression>? _scope;
        private LiteralExpression? _files;

        //--- Constructors ---
        public PackageDeclaration(LiteralExpression itemName) : base(itemName) { }

        //--- Properties --

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope ?? throw new InvalidOperationException();
            set => _scope = SetParent(value) ?? throw new ArgumentNullException();
        }

        // TODO: shouldn't this be List<LiteralExpression>?
        [SyntaxRequired]
        public LiteralExpression? Files {
            get => _files;
            set => _files = SetParent(value);
        }

        public List<KeyValuePair<string, string>> ResolvedFiles { get; set; } = new List<KeyValuePair<string, string>>();
        public bool HasSecretType => false;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Files?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Function")]
    public class FunctionDeclaration : AItemDeclaration, IScopedDeclaration, IConditionalResourceDeclaration {

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
            public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
                visitor.VisitStart(parent, this);
                SecurityGroupIds?.Visit(this, visitor);
                SubnetIds?.Visit(this, visitor);
                visitor.VisitEnd(parent, this);
            }
        }

        //--- Fields ---
        private SyntaxNodeCollection<LiteralExpression>? _scope;
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
            _environment = SetParent(new ObjectExpression());
            _properties = SetParent(new ObjectExpression());
            _sources = _sources = new SyntaxNodeCollection<AEventSourceDeclaration>();
            _pragmas = SetParent(new ListExpression());
        }

        //--- Properties ---

        [SyntaxOptional]
        public SyntaxNodeCollection<LiteralExpression> Scope {
            get => _scope ?? throw new InvalidOperationException();
            set => _scope = SetParent(value) ?? throw new ArgumentNullException();
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

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
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

    [SyntaxDeclarationKeyword("Mapping")]
    public class MappingDeclaration : AItemDeclaration {

        //--- Fields ---
        private ObjectExpression? _value;

        //--- Constructors ---
        public MappingDeclaration(LiteralExpression itemName) : base(itemName) { }

        //--- Properties ---

        [SyntaxRequired]
        public ObjectExpression? Value {
            get => _value;
            set => _value = SetParent(value);
        }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Value?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("ResourceType")]
    public class ResourceTypeDeclaration : AItemDeclaration {

        //--- Types ---
        public class PropertyTypeExpression : ASyntaxNode {

            //--- Fields ---
            private LiteralExpression? _name;
            private LiteralExpression? _type;
            private LiteralExpression? _required;

            //--- Properties ---

            [SyntaxRequired]
            public LiteralExpression? Name {
                get => _name;
                set => _name = SetParent(value);
            }

            [SyntaxRequired]
            public LiteralExpression? Type {
                get => _type;
                set => _type = SetParent(value);
            }

            [SyntaxOptional]
            public LiteralExpression? Required {
                get => _required;
                set => _required = SetParent(value);
            }

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

            //--- Fields ---
            private LiteralExpression? _name;
            private LiteralExpression? _type;

            //--- Properties ---

            [SyntaxRequired]
            public LiteralExpression? Name {
                get => _name;
                set => _name = SetParent(value);
            }

            [SyntaxRequired]
            public LiteralExpression? Type {
                get => _type;
                set => _type = SetParent(value);
            }

            //--- Methods ---
            public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
                visitor.VisitStart(parent, this);
                Name?.Visit(this, visitor);
                Type?.Visit(this, visitor);
                visitor.VisitEnd(parent, this);
            }
        }

        //--- Fields ---
        private LiteralExpression? _handler;
        private SyntaxNodeCollection<PropertyTypeExpression> _properties;
        private SyntaxNodeCollection<AttributeTypeExpression> _attributes;

        //--- Constructors ---
        public ResourceTypeDeclaration(LiteralExpression itemName) : base(itemName) {
            _properties = SetParent(new SyntaxNodeCollection<PropertyTypeExpression>());
            _attributes = SetParent(new SyntaxNodeCollection<AttributeTypeExpression>());
        }

        //--- Properties ---

        [SyntaxRequired]
        public LiteralExpression? Handler {
            get => _handler;
            set => _handler = SetParent(value);
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

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Handler?.Visit(this, visitor);
            Properties?.Visit(this, visitor);
            Attributes?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    [SyntaxDeclarationKeyword("Macro")]
    public class MacroDeclaration : AItemDeclaration, IResourceDeclaration {

        //--- Fields ---
        private LiteralExpression? _handler;

        //--- Constructors ---
        public MacroDeclaration(LiteralExpression keywordValue) : base(keywordValue) { }

        //--- Properties ---

        [SyntaxRequired]
        public LiteralExpression? Handler {
            get => _handler;
            set => _handler = SetParent(value);
        }

        public string CloudFormationType => "AWS::CloudFormation::Macro";

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ItemName.Visit(this, visitor);
            Handler?.Visit(this, visitor);
            Declarations?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }
}