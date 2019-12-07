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

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class AFunctionExpression : AExpression  { }

    public class Base64FunctionExpression : AFunctionExpression {

        // !Base64 VALUE
        // NOTE: You can use any function that returns a string inside the Fn::Base64 function.

        //--- Properties ---
        public AExpression Value { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Value?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class CidrFunctionExpression : AFunctionExpression {

        // !Cidr [ VALUE, VALUE, VALUE ]
        // NOTE: You can use the following functions in a Fn::Cidr function:
        //  - !Select
        //  - !Ref

        //--- Properties ---
        public AExpression IpBlock  { get; set; }
        public AExpression Count { get; set; }
        public AExpression CidrBits { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            IpBlock?.Visit(this, visitor);
            Count?.Visit(this, visitor);
            CidrBits?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class FindInMapFunctionExpression : AFunctionExpression {

        // !FindInMap [ STRING, VALUE, VALUE ]
        // NOTE: You can use the following functions in a Fn::FindInMap function:
        //  - Fn::FindInMap
        //  - Ref

        //--- Properties ---
        public LiteralExpression MapName { get; set; }
        public AExpression TopLevelKey { get; set; }
        public AExpression SecondLevelKey { get; set; }
        public MappingDeclaration ReferencedDeclaration { get; set; }
        public bool IsResolved => ReferencedDeclaration != null;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            MapName?.Visit(this, visitor);
            TopLevelKey?.Visit(this, visitor);
            SecondLevelKey?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class GetAttFunctionExpression : AFunctionExpression {

        // !GetAtt [ STRING, VALUE ]
        // NOTE: For the Fn::GetAtt logical resource name, you cannot use functions. You must specify a string that is a resource's logical ID.
        // For the Fn::GetAtt attribute name, you can use the Ref function.

        //--- Properties ---
        public LiteralExpression ReferenceName { get; set; }
        public AExpression AttributeName { get; set; }
        public AItemDeclaration ReferencedDeclaration { get; set; }
        public bool IsResolved => ReferencedDeclaration != null;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ReferenceName?.Visit(this, visitor);
            AttributeName?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class GetAZsFunctionExpression : AFunctionExpression {

        // !GetAZs VALUE
        // NOTE: You can use the Ref function in the Fn::GetAZs function.

        //--- Properties ---
        public AExpression Region { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Region?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class IfFunctionExpression : AFunctionExpression {

        // !If [ CONDITION, VALUE, VALUE ]
        // NOTE: AWS CloudFormation supports the Fn::If intrinsic function in the metadata attribute, update policy attribute, and property values in the Resources section and Outputs sections of a template.
        //  - Fn::Base64
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::Join
        //  - Fn::Select
        //  - Fn::Sub
        //  - Ref

        //--- Properties ---
        public AExpression Condition { get; set; }
        public AExpression IfTrue { get; set; }
        public AExpression IfFalse { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Condition?.Visit(this, visitor);
            IfTrue?.Visit(this, visitor);
            IfFalse?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ImportValueFunctionExpression : AFunctionExpression {

        // !ImportValue VALUE
        // NOTE: You can use the following functions in the Fn::ImportValue function. The value of these functions can't depend on a resource.
        //  - Fn::Base64
        //  - Fn::FindInMap
        //  - Fn::If
        //  - Fn::Join
        //  - Fn::Select
        //  - Fn::Split
        //  - Fn::Sub
        //  - Ref

        //--- Properties ---
        public AExpression SharedValueToImport { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            SharedValueToImport?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class JoinFunctionExpression : AFunctionExpression {

        // !Join [ STRING, VALUE ]
        // NOTE: For the Fn::Join delimiter, you cannot use any functions. You must specify a string value.
        //  For the Fn::Join list of values, you can use the following functions:
        //  - Fn::Base64
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::ImportValue
        //  - Fn::Join
        //  - Fn::Split
        //  - Fn::Select
        //  - Fn::Sub
        //  - Ref

        //--- Properties ---
        public LiteralExpression Separator { get; set; }
        public AExpression Values { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Separator?.Visit(this, visitor);
            Values?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class SelectFunctionExpression : AFunctionExpression {

        // !Select [ VALUE, VALUE ]
        // NOTE: For the Fn::Select index value, you can use the Ref and Fn::FindInMap functions.
        //  For the Fn::Select list of objects, you can use the following functions:
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::Split
        //  - Ref

        //--- Properties ---
        public AExpression Index { get; set; }
        public AExpression Values { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Index?.Visit(this, visitor);
            Values?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class SplitFunctionExpression : AFunctionExpression {

        // !Split [ VALUE, VALUE ]
        // NOTE: For the Fn::Split delimiter, you cannot use any functions. You must specify a string value.
        //  For the Fn::Split list of values, you can use the following functions:
        //  - Fn::Base64
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::ImportValue
        //  - Fn::Join
        //  - Fn::Select
        //  - Fn::Sub
        //  - Ref

        //--- Properties ---
        public AExpression Delimiter { get; set; }
        public AExpression SourceString { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Delimiter?.Visit(this, visitor);
            SourceString?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class SubFunctionExpression : AFunctionExpression {

        // !Sub VALUE
        // !Sub [ VALUE, OBJECT ]
        // NOTE: For the String parameter, you cannot use any functions. You must specify a string value.
        // For the VarName and VarValue parameters, you can use the following functions:
        //  - Fn::Base64
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::ImportValue
        //  - Fn::Join
        //  - Fn::Select
        //  - Ref

        //--- Properties ---
        public LiteralExpression FormatString { get; set; }
        public ObjectExpression Parameters { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            FormatString?.Visit(this, visitor);
            Parameters?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class TransformFunctionExpression : AFunctionExpression {

        // !Transform { Name: STRING, Parameters: OBJECT }
        // NOTE: AWS CloudFormation passes any intrinsic function calls included in Fn::Transform to the specified macro as literal strings.

        //--- Properties ---
        public LiteralExpression MacroName { get; set; }
        public ObjectExpression Parameters { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            MacroName?.Visit(this, visitor);
            Parameters?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ReferenceFunctionExpression : AFunctionExpression {

        // !Ref STRING
        // NOTE: You cannot use any functions in the Ref function. You must specify a string that is a resource logical ID.

        //--- Properties ---
        public LiteralExpression ReferenceName { get; set; }
        public AItemDeclaration ReferencedDeclaration { get; set; }
        public bool IsResolved => ReferencedDeclaration != null;

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ReferenceName?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }
}