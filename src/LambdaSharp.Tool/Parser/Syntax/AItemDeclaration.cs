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

using System.Collections.Generic;

namespace LambdaSharp.Tool.Parser.Syntax {

    public abstract class AItemDeclaration : ADeclaration {

        //--- Properties ---
        public LiteralExpression Description { get; set; }
    }

    public class ParameterDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Parameter { get; set; }

        [SyntaxOptional]
        public LiteralExpression Section { get; set; }

        [SyntaxOptional]
        public LiteralExpression Label { get; set; }

        [SyntaxOptional]
        public LiteralExpression Type { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Scope { get; set; }

        [SyntaxOptional]
        public LiteralExpression NoEcho { get; set; }

        [SyntaxOptional]
        public LiteralExpression Default { get; set; }

        [SyntaxOptional]
        public LiteralExpression ConstraintDescription { get; set; }

        [SyntaxOptional]
        public LiteralExpression AllowedPattern { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> AllowedValues { get; set; } = new List<LiteralExpression>();

        [SyntaxOptional]
        public LiteralExpression MaxLength { get; set; }

        [SyntaxOptional]
        public LiteralExpression MaxValue { get; set; }

        [SyntaxOptional]
        public LiteralExpression MinLength { get; set; }

        [SyntaxOptional]
        public LiteralExpression MinValue { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Allow { get; set; }

        [SyntaxOptional]
        public ObjectExpression Properties { get; set; }

        [SyntaxOptional]
        public ObjectExpression EncryptionContext { get; set; }

        [SyntaxOptional]
        public List<AValueExpression> Pragmas { get; set; } = new List<AValueExpression>();

        // NOTE (2019-10-26, bjorg): there is no syntax attribute, because items can only be added programmatically
        public List<AItemDeclaration> Items { get; set; } = new List<AItemDeclaration>();

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
            Items?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ImportDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Import { get; set; }

        [SyntaxOptional]
        public LiteralExpression Type { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Scope { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Allow { get; set; }

        [SyntaxRequired]
        public LiteralExpression Module { get; set; }

        [SyntaxOptional]
        public ObjectExpression EncryptionContext { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Import?.Visit(this, visitor);
            Type?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Allow?.Visit(this, visitor);
            Module?.Visit(this, visitor);
            EncryptionContext?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class VariableDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Variable { get; set; }

        [SyntaxOptional]
        public LiteralExpression Type { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Scope { get; set; }

        [SyntaxRequired]
        public AValueExpression Value { get; set; }

        [SyntaxOptional]
        public ObjectExpression EncryptionContext { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Variable?.Visit(this, visitor);
            Type?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Value?.Visit(this, visitor);
            EncryptionContext?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class GroupDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Group { get; set; }

        [SyntaxRequired]
        public List<AItemDeclaration> Items { get; set; } = new List<AItemDeclaration>();

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Group?.Visit(this, visitor);
            Items?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ConditionDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Condition { get; set; }

        [SyntaxRequired]
        public AConditionExpression Value { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Condition?.Visit(this, visitor);
            Value?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ResourceDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Resource { get; set; }

        [SyntaxOptional]
        public AConditionExpression If { get; set; } // TODO: -OR- name of a condition!

        [SyntaxOptional]
        public LiteralExpression Type { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Scope { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Allow { get; set; }

        [SyntaxOptional]
        public AValueExpression Value { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> DependsOn { get; set; } = new List<LiteralExpression>();

        [SyntaxOptional]
        public ObjectExpression Properties { get; set; }

        [SyntaxOptional]
        public LiteralExpression DefaultAttribute { get; set; }

        [SyntaxOptional]
        public List<AValueExpression> Pragmas { get; set; } = new List<AValueExpression>();

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
            visitor.VisitEnd(parent, this);
        }
    }

    public class NestedModuleDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Nested { get; set; }

        [SyntaxRequired]
        public LiteralExpression Module { get; set; }

        [SyntaxOptional]
        public List<LiteralExpression> DependsOn { get; set; } = new List<LiteralExpression>();

        [SyntaxOptional]
        public ObjectExpression Parameters { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Nested?.Visit(this, visitor);
            Module?.Visit(this, visitor);
            DependsOn?.Visit(this, visitor);
            Parameters?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class PackageDeclaration : AItemDeclaration {

        //--- Properties --

        [SyntaxKeyword]
        public LiteralExpression Package { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Scope { get; set; }

        [SyntaxRequired]
        public LiteralExpression Files { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Package?.Visit(this, visitor);
            Scope?.Visit(this, visitor);
            Files?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class FunctionDeclaration : AItemDeclaration {

        //--- Types ---
        public class VpcExpression : ASyntaxNode {

            //--- Properties ---

            [SyntaxRequired]
            public AValueExpression SecurityGroupIds { get;set; }

            [SyntaxRequired]
            public AValueExpression SubnetIds { get;set; }

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
        public LiteralExpression Function { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Scope { get; set; }

        [SyntaxOptional]
        public AConditionExpression If { get; set; }

        [SyntaxRequired]
        public AValueExpression Memory { get; set; }

        [SyntaxRequired]
        public AValueExpression Timeout { get; set; }

        [SyntaxOptional]
        public LiteralExpression Project { get; set; }

        [SyntaxOptional]
        public LiteralExpression Runtime { get; set; }

        // TODO: is this still useful?
        [SyntaxOptional]
        public LiteralExpression Language { get; set; }

        [SyntaxOptional]
        public LiteralExpression Handler { get; set; }

        [SyntaxOptional]
        public VpcExpression Vpc { get; set; }

        [SyntaxOptional]
        public ObjectExpression Environment { get; set; }

        [SyntaxOptional]
        public ObjectExpression Properties { get; set; }

        [SyntaxOptional]
        public List<AEventSourceDeclaration> Sources { get; set; } = new List<AEventSourceDeclaration>();

        [SyntaxOptional]
        public List<AValueExpression> Pragmas { get; set; } = new List<AValueExpression>();

        // NOTE (2019-10-26, bjorg): there is no syntax attribute, because items can only be added programmatically
        public List<AItemDeclaration> Items { get; set; } = new List<AItemDeclaration>();

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
            Items?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class MappingDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Mapping { get; set; }

        [SyntaxRequired]
        public ObjectExpression Value { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Mapping?.Visit(this, visitor);
            Value?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ResourceTypeDeclaration : AItemDeclaration {

        //--- Types ---
        public class PropertyTypeExpression : ASyntaxNode {

            //--- Properties ---

            [SyntaxKeyword]
            public LiteralExpression Name { get; set; }

            [SyntaxRequired]
            public LiteralExpression Type { get; set; }

            [SyntaxOptional]
            public LiteralExpression Required { get; set; }

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
            public LiteralExpression Name { get; set; }

            [SyntaxRequired]
            public LiteralExpression Type { get; set; }

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
        public LiteralExpression ResourceType { get; set; }

        [SyntaxRequired]
        public LiteralExpression Handler { get; set; }

        [SyntaxOptional]
        public List<PropertyTypeExpression> Properties { get; set; } = new List<PropertyTypeExpression>();

        [SyntaxOptional]
        public List<AttributeTypeExpression> Attributes { get; set; } = new List<AttributeTypeExpression>();

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ResourceType?.Visit(this, visitor);
            Handler?.Visit(this, visitor);
            Properties?.Visit(this, visitor);
            Attributes?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class MacroDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Macro { get; set; }

        [SyntaxRequired]
        public AValueExpression Handler { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Macro?.Visit(this, visitor);
            Handler?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    // TODO: consider replacing this with 'AValueExpression' instead
    public class TagListDeclaration : ASyntaxNode {

        //--- Properties ---
        public List<string> Tags { get; set; } = new List<string>();

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            visitor.VisitEnd(parent, this);
        }
    }
}