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

    public abstract class AValueExpression : ANode { }

    public class ObjectExpression : AValueExpression {

        //--- Properties ---
        public List<StringLiteral> Keys { get; set; } = new List<StringLiteral>();
        public Dictionary<string, AValueExpression> Values { get; set; } = new Dictionary<string, AValueExpression>();
    }

    public class ListExpression : AValueExpression {

        //--- Properties ---
        public List<AValueExpression> Values { get; set; } = new List<AValueExpression>();
    }

    public class LiteralExpression : AValueExpression {

        //--- Properties ---
        public string Value { get; set; }
    }

    public class AFunctionExpression : AValueExpression  { }

    public class Base64FunctionExpression : AFunctionExpression {

        // NOTE: You can use any function that returns a string inside the Fn::Base64 function.

        //--- Properties ---
        public AValueExpression Value { get; set; }
    }

    public class CidrFunctionExpression : AFunctionExpression {

        // NOTE: You can use the following functions in a Fn::Cidr function:
        //  - !Select
        //  - !Ref

        //--- Properties ---
        public AValueExpression IpBlock  { get; set; }
        public AValueExpression Count { get; set; }
        public AValueExpression CidrBits { get; set; }
    }

    public class FindInMapExpression : AFunctionExpression {

        // NOTE: You can use the following functions in a Fn::FindInMap function:
        //  - Fn::FindInMap
        //  - Ref

        //--- Properties ---
        public AValueExpression MapName { get; set; }
        public AValueExpression TopLevelKey { get; set; }
        public AValueExpression SecondLevelKey { get; set; }
    }

    public class GetAttFunctionExpression : AFunctionExpression {

        // NOTE: For the Fn::GetAtt logical resource name, you cannot use functions. You must specify a string that is a resource's logical ID.
        // For the Fn::GetAtt attribute name, you can use the Ref function.

        //--- Properties ---
        public LiteralExpression ResourceName { get; set; }
        public AValueExpression AttributeName { get; set; }
    }

    public class GetAZsFunctionExpression : AFunctionExpression {

        // NOTE: You can use the Ref function in the Fn::GetAZs function.

        //--- Properties ---
        public AValueExpression Region { get; set; }
    }

    public class IfFunctionExpression : AFunctionExpression {

        // NOTE: AWS CloudFormation supports the Fn::If intrinsic function in the metadata attribute, update policy attribute, and property values in the Resources section and Outputs sections of a template.

        //--- Properties ---

        // TODO: allow arbitrary condition expressions; instantiate condition item as needed
        public ConditionNameLiteralExpression ConditionName { get; set; }
        public AValueExpression IfTrue { get; set; }
        public AValueExpression IfFalse { get; set; }
    }

    public class ImportValueFunctionExpression : AFunctionExpression {

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
        public AValueExpression SharedValueToImport { get; set; }
    }

    public class JoinFunctionExpression : AFunctionExpression {

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
        public AValueExpression Values { get; set; }
    }

    public class SelectFunctionExpression : AFunctionExpression {

        // NOTE: For the Fn::Select index value, you can use the Ref and Fn::FindInMap functions.
        //  For the Fn::Select list of objects, you can use the following functions:
        //  - Fn::FindInMap
        //  - Fn::GetAtt
        //  - Fn::GetAZs
        //  - Fn::If
        //  - Fn::Split
        //  - Ref

        //--- Properties ---
        public AValueExpression Index { get; set; }
        public AValueExpression Values { get; set; }
    }

    public class SplitFunctionExpression : AFunctionExpression {

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
        public AValueExpression Delimiter { get; set; }
        public AValueExpression SourceString { get; set; }
    }

    public class SubFunctionExpression : AFunctionExpression {

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
    }

    public class TransformFunctionExpression : AFunctionExpression {

        // NOTE: AWS CloudFormation passes any intrinsic function calls included in Fn::Transform to the specified macro as literal strings.

        //--- Properties ---
        public LiteralExpression MacroName { get; set; }
        public ObjectExpression Parameters { get; set; }
    }

    public class ReferenceFunctionExpression : AFunctionExpression {

        // NOTE: You cannot use any functions in the Ref function. You must specify a string that is a resource logical ID.

        //--- Properties ---
        public LiteralExpression ResourceName { get; set; }
    }
}