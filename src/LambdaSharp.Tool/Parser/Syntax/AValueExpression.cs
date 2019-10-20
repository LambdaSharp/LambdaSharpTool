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
        public IList<(StringLiteral, AValueExpression)> Fields { get; set; }
    }

    public class AFunctionExpression : AValueExpression  { }

    public class IfFunctionExpression : AFunctionExpression {

        //--- Properties ---

        // TODO: allow arbitrary condition expressions; instantiate condition item as needed
        public ConditionNameLiteral Condition { get; set; }
        public AValueExpression IfTrue { get; set; }
        public AValueExpression IfFalse { get; set; }
    }

    public class GetAttFunctionExpression : AFunctionExpression {

        //--- Properties ---
        public AValueExpression ResourceWithAttributeReference { get; set; }
    }

    public class ImportFunctionExpression : AFunctionExpression {

        //--- Properties ---
        public AValueExpression Value { get; set; }
    }

    public class JoinFunctionExpression : AFunctionExpression {

        //--- Properties ---
        public AValueExpression Separator { get; set; }
        public AValueExpression Values { get; set; }
    }

    public class ReferenceFunctionExpression : AFunctionExpression {

        //--- Properties ---
        public AValueExpression ResourceReference { get; set; }
    }

    public class SelectFunctionExpression : AFunctionExpression {

        //--- Properties ---
        public AValueExpression Index { get; set; }
        public AValueExpression List { get; set; }
    }

    public class SubFunctionExpression : AFunctionExpression {

        //--- Properties ---
        public AValueExpression FormatString { get; set; }
        public ObjectExpression Arguments { get; set; }
    }

    public class SplitFunctionExpression : AFunctionExpression {

        //--- Properties ---
        public AValueExpression Delimiter { get; set; }
        public AValueExpression SourceString { get; set; }
    }

    public class FindInMapExpression : AFunctionExpression {

        //--- Properties ---
        public AValueExpression MapName { get; set; }
        public AValueExpression TopLevelKey { get; set; }
        public AValueExpression SecondLevelKey { get; set; }
    }

    public class TransformExpression : AFunctionExpression {

        //--- Properties ---
        public StringLiteral MacroName { get; set; }
        public ObjectExpression Parameters { get; set; }
    }
}