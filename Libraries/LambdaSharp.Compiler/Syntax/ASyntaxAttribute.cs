/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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

namespace LambdaSharp.Compiler.Syntax {

    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ASyntaxAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SyntaxRequiredAttribute : ASyntaxAttribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SyntaxOptionalAttribute : ASyntaxAttribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SyntaxHiddenAttribute : ASyntaxAttribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SyntaxDeclarationKeywordAttribute : Attribute {

        //--- Constructors ---
        public SyntaxDeclarationKeywordAttribute(string keyword) => Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
        public SyntaxDeclarationKeywordAttribute(string keyword, Type type) : this(keyword) => Type = type ?? throw new ArgumentNullException(nameof(type));

        //--- Properties ---
        public string Keyword { get; }
        public Type? Type { get; }
    }
}