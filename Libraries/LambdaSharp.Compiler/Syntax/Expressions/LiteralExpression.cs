/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
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

namespace LambdaSharp.Compiler.Syntax.Expressions {

    public enum LiteralType {
        String,
        Integer,
        Float,
        Bool,
        Timestamp,
        Null
    }

    public sealed class LiteralExpression : AValueExpression {

        //--- Constructors ---
        public LiteralExpression(string value, LiteralType type, bool fromExistsExpression = false) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Type = type;
            FromExistsExpression = fromExistsExpression;
        }

        //--- Properties ---
        public string Value { get; }
        public LiteralType Type { get; }
        public bool FromExistsExpression { get; }
        public bool IsString => Type == LiteralType.String;
        public bool IsInteger => Type == LiteralType.Integer;
        public bool IsFloat => Type == LiteralType.Float;
        public bool IsBool => Type == LiteralType.Bool;
        public bool IsTimestamp => Type == LiteralType.Timestamp;
        public bool IsNull => Type == LiteralType.Null;

        //--- Methods ---
        public bool? AsBool() => (Type == LiteralType.Bool) ? bool.Parse(Value) : (bool?)null;
        public int? AsInt() => (Type == LiteralType.Integer) ? int.Parse(Value) : (int?)null;
        public override ASyntaxNode CloneNode() => new LiteralExpression(Value, Type);
    }
}