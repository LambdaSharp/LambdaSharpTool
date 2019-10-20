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
        public override string Keyword => "Parameter";
        public StringLiteral Parameter { get; set; }
        public StringLiteral Section { get; set; }
        public StringLiteral Label { get; set; }
        public StringLiteral Type { get; set; }
        public ScopeExpression Scope { get; set; }
        public BoolLiteral NoEcho { get; set; }
        public StringLiteral Default { get; set; }
        public StringLiteral ConstraintDescription { get; set; }
        public StringLiteral AllowedPattern { get; set; }
        public DeclarationList<StringLiteral> AllowedValues { get; set; }
        public IntLiteral MaxLength { get; set; }
        public IntLiteral MaxValue { get; set; }
        public IntLiteral MinLength { get; set; }
        public IntLiteral MinValue { get; set; }
        public AllowDeclaration Allow { get; set; }
        public PropertiesExpression Properties { get; set; }
        public ObjectExpression EncryptionContext { get; set; }
        public DeclarationList<PragmaExpression> Pragmas { get; set; }
    }

    public class ImportDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Import";
        public StringLiteral Import { get; set; }
        public StringLiteral Type { get; set; }
        public ScopeExpression Scope { get; set; }
        public DeclarationList<StringLiteral> AllowedValues { get; set; }
        public StringLiteral Module { get; set; }
        public ObjectExpression EncryptionContext { get; set; }
    }

    public class VariableDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Variable";
        public StringLiteral Variable { get; set; }
        public StringLiteral Type { get; set; }
        public ScopeExpression Scope { get; set; }
        public AValueExpression Value { get; set; }
        public ObjectExpression EncryptionContext { get; set; }
    }

    public class GroupDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Group";
        public StringLiteral Group { get; set; }
        public DeclarationList<AItemDeclaration> Items { get; set; }
    }

    public class ConditionDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Condition";
        public StringLiteral Condition { get; set; }
        public AConditionExpression Value { get; set; }
    }

    public class ResourceDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Resource";
        public StringLiteral Resource { get; set; }
        public AConditionExpression If { get; set; } // -OR- name of a condition!
        public StringLiteral Type { get; set; }
        public ScopeExpression Scope { get; set; }
        public AllowDeclaration Allow { get; set; }
        public AValueExpression Value { get; set; }
        public DeclarationList<StringLiteral> DependsOn { get; set; }
        public PropertiesExpression Properties { get; set; }
        public StringLiteral DefaultAttribute { get; set; }
        public DeclarationList<PragmaExpression> Pragmas { get; set; }
    }

    public class NestedModuleDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Nested";
        public StringLiteral Nested { get; set; }
        public ModuleLiteral Module { get; set; }
        public DeclarationList<StringLiteral> DependsOn { get; set; }
        public ObjectExpression Parameters { get; set; }
    }

    public class PackageDeclaration : AItemDeclaration {

        //--- Properties --
        public override string Keyword => "Package";
        public StringLiteral Package { get; set; }
        public ScopeExpression Scope { get; set; }
        public DeclarationList<StringLiteral> Files { get; set; }
    }

    public class FunctionDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Function";
        public StringLiteral Function { get; set; }
        public ScopeExpression Scope { get; set; }
        public AConditionExpression If { get; set; } // -OR- name of a condition!
        public IntLiteral Memory { get; set; }
        public IntLiteral Timeout { get; set; }
        public StringLiteral Project { get; set; }
        public StringLiteral Runtime { get; set; }
        public StringLiteral Language { get; set; }
        public StringLiteral Handler { get; set; }
        public VpcExpression Vpc { get; set; }
        public ObjectExpression Environment { get; set; }
        public PropertiesExpression Properties { get; set; }
        public DeclarationList<AEventSourceDeclaration> Sources { get; set; }
        public DeclarationList<PragmaExpression> Pragmas { get; set; }
    }

    public class VpcExpression : ANode {

        //--- Properties ---
        // TODO:
    }

    public class MappingDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Mapping";
        public StringLiteral Mapping { get; set; }
        // TODO:
    }

    public class ResourceTypeDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "ResourceType";
        public StringLiteral ResourceType { get; set; }
        public StringLiteral Handler { get; set; }
        public DeclarationList<PropertyTypeDeclaration> Properties { get; set; }
        public DeclarationList<AttributeTypeDeclaration> Attributes { get; set; }
        public DeclarationList<TypeDeclaration> Types { get; set; }
    }

    public class PropertyTypeDeclaration : ADeclaration {

        //--- Properties ---
        public override string Keyword => "Name";
        public StringLiteral Name { get; set; }
        // TODO:
    }

    public class AttributeTypeDeclaration : ADeclaration {

        //--- Properties ---
        public override string Keyword => "Name";
        public StringLiteral Name { get; set; }
        // TODO:
    }

    public class TypeDeclaration : ADeclaration {

        //--- Properties ---
        public override string Keyword => "Name";
        public StringLiteral Name { get; set; }
        // TODO:
    }

    public class MacroDeclaration : AItemDeclaration {

        //--- Properties ---
        public override string Keyword => "Macro";
        public StringLiteral Macro { get; set; }
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