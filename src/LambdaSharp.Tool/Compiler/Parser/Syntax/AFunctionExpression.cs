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

namespace LambdaSharp.Tool.Compiler.Parser.Syntax {

    public abstract class AFunctionExpression : AExpression { }

    public class Base64FunctionExpression : AFunctionExpression {

        //--- Fields ---
        private AExpression? _value;

        // !Base64 VALUE
        // NOTE: You can use any function that returns a string inside the Fn::Base64 function.

        //--- Properties ---
        public AExpression Value {
            get => _value ?? throw new InvalidOperationException();
            set => _value = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Value = Value?.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new Base64FunctionExpression {
            Value = Value.Clone()
        };
    }

    public class CidrFunctionExpression : AFunctionExpression {

        // !Cidr [ VALUE, VALUE, VALUE ]
        // NOTE: You can use the following functions in a Fn::Cidr function:
        //  - !Select
        //  - !Ref

        //--- Fields ---
        private AExpression? _ipBlock;
        private AExpression? _count;
        private AExpression? _cidrBits;

        //--- Properties ---
        public AExpression IpBlock {
            get => _ipBlock ?? throw new InvalidOperationException();
            set => _ipBlock = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression Count {
            get => _count ?? throw new InvalidOperationException();
            set => _count = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression CidrBits {
            get => _cidrBits ?? throw new InvalidOperationException();
            set => _cidrBits = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            IpBlock = IpBlock.Visit(this, visitor) ?? throw new NullValueException();
            Count = Count.Visit(this, visitor) ?? throw new NullValueException();
            CidrBits = CidrBits.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new CidrFunctionExpression {
            IpBlock = IpBlock.Clone(),
            Count = Count.Clone(),
            CidrBits = CidrBits.Clone()
        };
    }

    public class FindInMapFunctionExpression : AFunctionExpression {

        // !FindInMap [ STRING, VALUE, VALUE ]
        // NOTE: You can use the following functions in a Fn::FindInMap function:
        //  - Fn::FindInMap
        //  - Ref

        //--- Fields ---
        private LiteralExpression? _mapName;
        private AExpression? _topLevelKey;
        private AExpression? _secondLevelKey;
        private MappingDeclaration? _referencedDeclaration;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression MapName {
            get => _mapName ?? throw new InvalidOperationException();
            set => _mapName = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression TopLevelKey {
            get => _topLevelKey ?? throw new InvalidOperationException();
            set => _topLevelKey = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression SecondLevelKey {
            get => _secondLevelKey ?? throw new InvalidOperationException();
            set => _secondLevelKey = SetParent(value) ?? throw new ArgumentNullException();
        }

        public MappingDeclaration? ReferencedDeclaration {
            get => _referencedDeclaration;
            set {
                if(_referencedDeclaration != null) {
                    _referencedDeclaration.UntrackDependency(this);
                }
                _referencedDeclaration = value;
                if(_referencedDeclaration != null) {
                    ParentItemDeclaration?.TrackDependency(_referencedDeclaration, this);
                }
            }
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            MapName = MapName.Visit(this, visitor) ?? throw new NullValueException();
            TopLevelKey = TopLevelKey.Visit(this, visitor) ?? throw new NullValueException();
            SecondLevelKey = SecondLevelKey.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new FindInMapFunctionExpression {
            MapName = MapName.Clone(),
            TopLevelKey = TopLevelKey.Clone(),
            SecondLevelKey = SecondLevelKey.Clone(),
            ReferencedDeclaration = ReferencedDeclaration
        };
    }

    public class GetAttFunctionExpression : AFunctionExpression {

        // !GetAtt [ STRING, VALUE ]
        // NOTE: For the Fn::GetAtt logical resource name, you cannot use functions. You must specify a string that is a resource's logical ID.
        // For the Fn::GetAtt attribute name, you can use the Ref function.

        //--- Fields ---
        private LiteralExpression? _referenceName;
        private AExpression? _attributeName;
        private AItemDeclaration? _referencedDeclaration;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression ReferenceName {
            get => _referenceName ?? throw new InvalidOperationException();
            set => _referenceName = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression AttributeName {
            get => _attributeName ?? throw new InvalidOperationException();
            set => _attributeName = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AItemDeclaration? ReferencedDeclaration {
            get => _referencedDeclaration;
            set {
                if(_referencedDeclaration != null) {
                    _referencedDeclaration.UntrackDependency(this);
                }
                _referencedDeclaration = value;
                if(_referencedDeclaration != null) {
                    ParentItemDeclaration?.TrackDependency(_referencedDeclaration, this);
                }
            }
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ReferenceName = ReferenceName.Visit(this, visitor) ?? throw new NullValueException();
            AttributeName = AttributeName.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new GetAttFunctionExpression {
            ReferenceName = ReferenceName.Clone(),
            AttributeName = AttributeName.Clone(),
            ReferencedDeclaration = ReferencedDeclaration
        };
    }

    public class GetAZsFunctionExpression : AFunctionExpression {

        // !GetAZs VALUE
        // NOTE: You can use the Ref function in the Fn::GetAZs function.

        //--- Fields ---
        private AExpression? _region;

        //--- Properties ---
        public AExpression Region {
            get => _region ?? throw new InvalidOperationException();
            set => _region = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Region = Region.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new GetAZsFunctionExpression {
            Region = Region.Clone()
        };
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

        //--- Fields ---
        private AExpression? _condition;
        private AExpression? _ifTrue;
        private AExpression? _ifFalse;

        //--- Properties ---
        public AExpression Condition {
            get => _condition ?? throw new InvalidOperationException();
            set => _condition = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression IfTrue {
            get => _ifTrue ?? throw new InvalidOperationException();
            set => _ifTrue = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression IfFalse {
            get => _ifFalse ?? throw new InvalidOperationException();
            set => _ifFalse = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Condition = Condition.Visit(this, visitor) ?? throw new NullValueException();
            IfTrue = IfTrue.Visit(this, visitor) ?? throw new NullValueException();
            IfFalse = IfFalse.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new IfFunctionExpression {
            Condition = Condition.Clone(),
            IfTrue = IfTrue.Clone(),
            IfFalse = IfFalse.Clone()
        };
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

        //--- Fields ---
        private AExpression? _sharedValueToImport;

        //--- Properties ---
        public AExpression SharedValueToImport {
            get => _sharedValueToImport ?? throw new InvalidOperationException();
            set => _sharedValueToImport = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            SharedValueToImport = SharedValueToImport.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new ImportValueFunctionExpression {
            SharedValueToImport = SharedValueToImport.Clone()
        };
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

        //--- Fields ---
        private LiteralExpression? _separator;
        private AExpression? _values;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression Separator {
            get => _separator ?? throw new InvalidOperationException();
            set => _separator = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression Values {
            get => _values ?? throw new InvalidOperationException();
            set => _values = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Separator = Separator.Visit(this, visitor) ?? throw new NullValueException();
            Values = Values.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new JoinFunctionExpression {
            Separator = Separator.Clone(),
            Values = Values.Clone()
        };
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

        //--- Fields ---
        private AExpression? _index;
        private AExpression? _values;

        //--- Properties ---
        public AExpression Index {
            get => _index ?? throw new InvalidOperationException();
            set => _index = SetParent(value) ?? throw new ArgumentNullException();
        }

        // TODO: use [DisallowNull] or make non-null?
        public AExpression? Values {
            get => _values;
            set => _values = SetParent(value);
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Index = Index.Visit(this, visitor) ?? throw new NullValueException();
            Values = Values?.Visit(this, visitor);
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new SelectFunctionExpression {
            Index = Index.Clone(),
            Values = Values?.Clone()
        };
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

        //--- Fields ---
        private LiteralExpression? _delimiter;
        private AExpression? _sourceString;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression Delimiter {
            get => _delimiter ?? throw new InvalidOperationException();
            set => _delimiter = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AExpression SourceString {
            get => _sourceString ?? throw new InvalidOperationException();
            set => _sourceString = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Delimiter = Delimiter.Visit(this, visitor) ?? throw new NullValueException();
            SourceString = SourceString.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new SplitFunctionExpression {
            Delimiter = Delimiter.Clone(),
            SourceString = SourceString.Clone()
        };
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

        //--- Fields ---
        private LiteralExpression? _formatString;
        private ObjectExpression _parameters;

        //--- Constructors ---
        public SubFunctionExpression() {
            _parameters = SetParent(new ObjectExpression());
        }

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression FormatString {
            get => _formatString ?? throw new InvalidOperationException();
            set => _formatString = SetParent(value) ?? throw new ArgumentNullException();
        }

        public ObjectExpression Parameters {
            get => _parameters ?? throw new InvalidOperationException();
            set => _parameters = SetParent(value) ?? throw new ArgumentNullException();
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            FormatString = FormatString.Visit(this, visitor) ?? throw new NullValueException();
            Parameters = Parameters.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new SubFunctionExpression {
            FormatString = FormatString.Clone(),
            Parameters = Parameters.Clone()
        };
    }

    public class TransformFunctionExpression : AFunctionExpression {

        // !Transform { Name: STRING, Parameters: OBJECT }
        // NOTE: AWS CloudFormation passes any intrinsic function calls included in Fn::Transform to the specified macro as literal strings.

        //--- Fields ---
        private LiteralExpression? _macroName;
        private ObjectExpression? _parameters;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression MacroName {
            get => _macroName ?? throw new InvalidOperationException();
            set => _macroName = SetParent(value) ?? throw new ArgumentNullException();
        }

        public ObjectExpression? Parameters {
            get => _parameters;
            set => _parameters = SetParent(value);
        }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            MacroName = MacroName.Visit(this, visitor) ?? throw new NullValueException();
            Parameters = Parameters?.Visit(this, visitor);
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new TransformFunctionExpression {
            MacroName = MacroName.Clone(),
            Parameters = Parameters?.Clone()
        };
    }

    public class ReferenceFunctionExpression : AFunctionExpression {

        // !Ref STRING
        // NOTE: You cannot use any functions in the Ref function. You must specify a string that is a resource logical ID.

        //--- Fields ---
        private LiteralExpression? _referenceName;
        private AItemDeclaration? _referencedDeclaration;

        //--- Properties ---

        // TODO: allow AExpression, but then validate that after optimization it is a string literal
        public LiteralExpression ReferenceName {
            get => _referenceName ?? throw new InvalidOperationException();
            set => _referenceName = SetParent(value) ?? throw new ArgumentNullException();
        }

        public AItemDeclaration? ReferencedDeclaration {
            get => _referencedDeclaration;
            set {
                if(_referencedDeclaration != null) {
                    _referencedDeclaration.UntrackDependency(this);
                }
                _referencedDeclaration = value;
                if(_referencedDeclaration != null) {
                    ParentItemDeclaration?.TrackDependency(_referencedDeclaration, this);
                }
            }
        }

        public bool Resolved { get; set; }

        //--- Methods ---
        public override ASyntaxNode? VisitNode(ASyntaxNode? parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            ReferenceName = ReferenceName.Visit(this, visitor) ?? throw new NullValueException();
            return visitor.VisitEnd(parent, this);
        }

        public override ASyntaxNode CloneNode() => new ReferenceFunctionExpression {
            ReferenceName = ReferenceName.Clone(),
            ReferencedDeclaration = ReferencedDeclaration,
            Resolved = Resolved
        };
    }
}