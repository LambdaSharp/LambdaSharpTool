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

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.Validators {

    public interface IModuleValidatorDependencyProvider {

        //--- Properties ---
        ILogger Logger { get; }

        //--- Methods ---
        bool IsResourceType(string type);
        bool TryGetResourceType(string typeName, out ResourceType resourceType);
        Task<string> ConvertKmsAliasToArn(string alias);
        void DeclareItem(AItemDeclaration itemDeclaration);
        bool TryGetItem(string fullname, [NotNullWhen(true)] out AItemDeclaration? itemDeclaration);
    }
}