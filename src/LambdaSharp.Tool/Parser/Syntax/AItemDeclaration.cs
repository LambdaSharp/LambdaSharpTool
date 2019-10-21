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
        public StringLiteral Description { get; set; }
    }

    public class ParameterDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Parameter { get; set; }

        [SyntaxOptional]
        public StringLiteral Section { get; set; }

        [SyntaxOptional]
        public StringLiteral Label { get; set; }

        [SyntaxOptional]
        public StringLiteral Type { get; set; }

        [SyntaxOptional]
        public ScopeExpression Scope { get; set; }

        [SyntaxOptional]
        public BoolLiteral NoEcho { get; set; }

        [SyntaxOptional]
        public StringLiteral Default { get; set; }

        [SyntaxOptional]
        public StringLiteral ConstraintDescription { get; set; }

        [SyntaxOptional]
        public StringLiteral AllowedPattern { get; set; }

        [SyntaxOptional]
        public DeclarationList<StringLiteral> AllowedValues { get; set; }

        [SyntaxOptional]
        public IntLiteral MaxLength { get; set; }

        [SyntaxOptional]
        public IntLiteral MaxValue { get; set; }

        [SyntaxOptional]
        public IntLiteral MinLength { get; set; }

        [SyntaxOptional]
        public IntLiteral MinValue { get; set; }

        [SyntaxOptional]
        public AllowDeclaration Allow { get; set; }

        [SyntaxOptional]
        public PropertiesExpression Properties { get; set; }

        [SyntaxOptional]
        public ObjectExpression EncryptionContext { get; set; }

        [SyntaxOptional]
        public DeclarationList<PragmaExpression> Pragmas { get; set; }
    }

    public class ImportDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Import { get; set; }

        [SyntaxOptional]
        public StringLiteral Type { get; set; }

        [SyntaxOptional]
        public ScopeExpression Scope { get; set; }

        [SyntaxOptional]
        public DeclarationList<StringLiteral> AllowedValues { get; set; }

        [SyntaxRequired]
        public ModuleLiteral Module { get; set; }

        [SyntaxOptional]
        public ObjectExpression EncryptionContext { get; set; }
    }

    public class VariableDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Variable { get; set; }

        [SyntaxOptional]
        public StringLiteral Type { get; set; }

        [SyntaxOptional]
        public ScopeExpression Scope { get; set; }

        [SyntaxRequired]
        public AValueExpression Value { get; set; }

        [SyntaxOptional]
        public ObjectExpression EncryptionContext { get; set; }
    }

    public class GroupDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Group { get; set; }

        [SyntaxRequired]
        public DeclarationList<AItemDeclaration> Items { get; set; }
    }

    public class ConditionDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Condition { get; set; }

        [SyntaxRequired]
        public AConditionExpression Value { get; set; }
    }

    public class ResourceDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Resource { get; set; }

        [SyntaxOptional]
        public AConditionExpression If { get; set; } // -OR- name of a condition!

        [SyntaxOptional]
        public StringLiteral Type { get; set; }

        [SyntaxOptional]
        public ScopeExpression Scope { get; set; }

        [SyntaxOptional]
        public AllowDeclaration Allow { get; set; }

        [SyntaxOptional]
        public AValueExpression Value { get; set; }

        [SyntaxOptional]
        public DeclarationList<StringLiteral> DependsOn { get; set; }

        [SyntaxOptional]
        public PropertiesExpression Properties { get; set; }

        [SyntaxOptional]
        public StringLiteral DefaultAttribute { get; set; }

        [SyntaxOptional]
        public DeclarationList<PragmaExpression> Pragmas { get; set; }
    }

    public class NestedModuleDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Nested { get; set; }

        [SyntaxRequired]
        public ModuleLiteral Module { get; set; }

        [SyntaxOptional]
        public DeclarationList<StringLiteral> DependsOn { get; set; }

        [SyntaxOptional]
        public ObjectExpression Parameters { get; set; }
    }

    public class PackageDeclaration : AItemDeclaration {

        //--- Properties --

        [SyntaxKeyword]
        public StringLiteral Package { get; set; }

        [SyntaxOptional]
        public ScopeExpression Scope { get; set; }

        [SyntaxRequired]
        public DeclarationList<StringLiteral> Files { get; set; }
    }

    public class FunctionDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Function { get; set; }

        [SyntaxOptional]
        public ScopeExpression Scope { get; set; }

        [SyntaxOptional]
        public AConditionExpression If { get; set; } // -OR- name of a condition!

        [SyntaxRequired]
        public AValueExpression Memory { get; set; }

        [SyntaxRequired]
        public AValueExpression Timeout { get; set; }

        [SyntaxOptional]
        public StringLiteral Project { get; set; }

        [SyntaxOptional]
        public StringLiteral Runtime { get; set; }

        [SyntaxOptional]
        public StringLiteral Language { get; set; }

        [SyntaxOptional]
        public StringLiteral Handler { get; set; }

        [SyntaxOptional]
        public VpcExpression Vpc { get; set; }

        [SyntaxOptional]
        public ObjectExpression Environment { get; set; }

        [SyntaxOptional]
        public PropertiesExpression Properties { get; set; }

        [SyntaxOptional]
        public DeclarationList<AEventSourceDeclaration> Sources { get; set; }

        [SyntaxOptional]
        public DeclarationList<PragmaExpression> Pragmas { get; set; }
    }

    public class VpcExpression : ANode {

        //--- Properties ---
        // TODO:
    }

    public class MappingDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Mapping { get; set; }
        // TODO:
    }

    public class ResourceTypeDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral ResourceType { get; set; }

        [SyntaxRequired]
        public StringLiteral Handler { get; set; }

        [SyntaxOptional]
        public DeclarationList<PropertyTypeDeclaration> Properties { get; set; }

        [SyntaxOptional]
        public DeclarationList<AttributeTypeDeclaration> Attributes { get; set; }

        [SyntaxOptional]
        public DeclarationList<TypeDeclaration> Types { get; set; }
    }

    public class PropertyTypeDeclaration : ADeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Name { get; set; }
        // TODO:
    }

    public class AttributeTypeDeclaration : ADeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Name { get; set; }
        // TODO:
    }

    public class TypeDeclaration : ADeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Name { get; set; }
        // TODO:
    }

    public class MacroDeclaration : AItemDeclaration {

        //--- Properties ---

        [SyntaxKeyword]
        public StringLiteral Macro { get; set; }

        [SyntaxRequired]
        public AValueExpression Handler { get; set; }
    }

    public class ScopeExpression : ANode {

        //--- Properties ---
        public IList<StringLiteral> Values { get; set; }
    }

    public class AllowDeclaration : ANode {

        //--- Properties ---
        public IList<StringLiteral> Values { get; set; }
    }

    public class PropertiesExpression : ANode {

        //--- Properties ---
        // TODO:
    }
}