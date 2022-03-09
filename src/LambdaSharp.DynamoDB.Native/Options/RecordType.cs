/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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
using System.Diagnostics.CodeAnalysis;

namespace LambdaSharp.DynamoDB.Native.Options {

    // TODO (2021-07-11, bjorg): not ready to be public
    internal class RecordType {

        //--- Fields ---
        private Type? _type;
        private string? _shortTypeName;

        //--- Properties ---
        public Type Type {
            get => _type ?? throw new InvalidOperationException();
            set => _type = value ?? throw new ArgumentNullException();
        }

        [AllowNull]
        public string ShortTypeName {
            get => _shortTypeName ?? FullTypeName;
            set => _shortTypeName = value;
        }

        public string FullTypeName => Type.FullName ?? throw new InvalidOperationException();
    }

    // TODO (2021-07-11, bjorg): not ready to be public
    internal class RecordType<TRecord> : RecordType where TRecord : class {

        //--- Constructors ---
        public RecordType( ) => Type = typeof(TRecord);
    }
}
