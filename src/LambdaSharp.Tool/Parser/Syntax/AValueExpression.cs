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
using System.Linq;

namespace LambdaSharp.Tool.Parser.Syntax {

    public abstract class AValueExpression : ASyntaxNode { }

    public class ObjectExpression : AValueExpression {

        //--- Types ---
        public class KeyValuePair : ASyntaxNode {

            //--- Properties ---
            public string Key { get; set; }
            public AValueExpression Value { get; set; }

            //--- Methods ---
            public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
                visitor.VisitStart(parent, this);
                Value.Visit(this, visitor);
                visitor.VisitEnd(parent, this);
            }
        }

        //--- Properties ---
        public List<KeyValuePair> Items { get; set; } = new List<KeyValuePair>();

        //--- Operators ---
        public AValueExpression this[string key] => Items.First(item => item.Key == key).Value;

        //--- Methods ---
        public bool TryGetValue(string key, out AValueExpression value) {
            var found = Items.FirstOrDefault(item => item.Key == key);
            value = found?.Value;
            return found != null;
        }

        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Items.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class ListExpression : AValueExpression {

        //--- Properties ---
        public List<AValueExpression> Items { get; set; } = new List<AValueExpression>();

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            Items?.Visit(this, visitor);
            visitor.VisitEnd(parent, this);
        }
    }

    public class LiteralExpression : AValueExpression {

        //--- Properties ---
        public string Value { get; set; }

        //--- Methods ---
        public override void Visit(ASyntaxNode parent, ISyntaxVisitor visitor) {
            visitor.VisitStart(parent, this);
            visitor.VisitEnd(parent, this);
        }
    }
}