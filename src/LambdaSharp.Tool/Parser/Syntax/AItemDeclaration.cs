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
        public ListOf<LiteralExpression> AllowedValues { get; set; }

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
        public ListOf<AValueExpression> Pragmas { get; set; }
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
        public ListOf<LiteralExpression> AllowedValues { get; set; }

        [SyntaxRequired]
        public LiteralExpression Module { get; set; }

        [SyntaxOptional]
        public ObjectExpression EncryptionContext { get; set; }
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
    }

    public class GroupDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Group { get; set; }

        [SyntaxRequired]
        public ListOf<AItemDeclaration> Items { get; set; }
    }

    public class ConditionDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Condition { get; set; }

        [SyntaxRequired]
        public AConditionExpression Value { get; set; }
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
        public ListOf<LiteralExpression> DependsOn { get; set; }

        [SyntaxOptional]
        public ObjectExpression Properties { get; set; }

        [SyntaxOptional]
        public LiteralExpression DefaultAttribute { get; set; }

        [SyntaxOptional]
        public ListOf<AValueExpression> Pragmas { get; set; }
    }

    public class NestedModuleDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Nested { get; set; }

        [SyntaxRequired]
        public LiteralExpression Module { get; set; }

        [SyntaxOptional]
        public ListOf<LiteralExpression> DependsOn { get; set; }

        [SyntaxOptional]
        public ObjectExpression Parameters { get; set; }
    }

    public class PackageDeclaration : AItemDeclaration {

        //--- Properties --

        [SyntaxKeyword]
        public LiteralExpression Package { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Scope { get; set; }

        [SyntaxRequired]
        public ListOf<LiteralExpression> Files { get; set; }
    }

    public class FunctionDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Function { get; set; }

        [SyntaxOptional]
        public TagListDeclaration Scope { get; set; }

        [SyntaxOptional]
        public AConditionExpression If { get; set; } // -OR- name of a condition!

        [SyntaxRequired]
        public AValueExpression Memory { get; set; }

        [SyntaxRequired]
        public AValueExpression Timeout { get; set; }

        [SyntaxOptional]
        public LiteralExpression Project { get; set; }

        [SyntaxOptional]
        public LiteralExpression Runtime { get; set; }

        [SyntaxOptional]
        public LiteralExpression Language { get; set; }

        [SyntaxOptional]
        public LiteralExpression Handler { get; set; }

        [SyntaxOptional]
        public VpcDeclaration Vpc { get; set; }

        [SyntaxOptional]
        public ObjectExpression Environment { get; set; }

        [SyntaxOptional]
        public ObjectExpression Properties { get; set; }

        [SyntaxOptional]
        public ListOf<AEventSourceDeclaration> Sources { get; set; }

        [SyntaxOptional]
        public ListOf<AValueExpression> Pragmas { get; set; }
    }

    public class VpcDeclaration : ADeclaration {

        //--- Properties ---

        [SyntaxRequired]
        public AValueExpression SecurityGroupIds { get;set; }

        [SyntaxRequired]
        public AValueExpression SubnetIds { get;set; }
    }

    public class MappingDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Mapping { get; set; }

        [SyntaxRequired]
        public ObjectExpression Value { get; set; }
    }

    public class ResourceTypeDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression ResourceType { get; set; }

        [SyntaxRequired]
        public LiteralExpression Handler { get; set; }

        [SyntaxOptional]
        public ListOf<PropertyTypeDeclaration> Properties { get; set; }

        [SyntaxOptional]
        public ListOf<AttributeTypeDeclaration> Attributes { get; set; }
    }

    public class PropertyTypeDeclaration : ADeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Name { get; set; }

        [SyntaxRequired]
        public LiteralExpression Type { get; set; }

        [SyntaxOptional]
        public LiteralExpression Required { get; set; }
    }

    public class AttributeTypeDeclaration : ADeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Name { get; set; }

        [SyntaxRequired]
        public LiteralExpression Type { get; set; }
    }

    public class MacroDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public LiteralExpression Macro { get; set; }

        [SyntaxRequired]
        public AValueExpression Handler { get; set; }
    }

    public class TagListDeclaration : ASyntaxNode {

        //--- Properties ---
        public List<string> Tags { get; set; } = new List<string>();
    }
}