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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using LambdaSharp.CloudFormation.TypeSystem;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;
using LambdaSharp.Modules;
using LambdaSharp.Modules.Metadata;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    public interface ISyntaxProcessorDependencyProvider {

        //--- Properties ---
        ILogger Logger { get; }
        IEnumerable<AItemDeclaration> Declarations { get; }

        //--- Methods ---
        bool TryGetResourceType(string typeName, [NotNullWhen(true)] out IResourceType? resourceType);
        Task<string> ConvertKmsAliasToArn(string alias);
        void DeclareItem(ASyntaxNode? parent, AItemDeclaration itemDeclaration);
        void DeclareReferenceExpression(string fullname, AExpression expression);
        void DeclareValueExpression(string fullname, AExpression expression);
        bool TryGetItem(string fullname, [NotNullWhen(true)] out AItemDeclaration? itemDeclaration);
        bool TryGetReferenceExpression(string fullname, [NotNullWhen(true)] out AExpression? expression);
        bool TryGetValueExpression(string fullname, [NotNullWhen(true)] out AExpression? expression);
        Task<ModuleManifest> ResolveModuleInfoAsync(ModuleManifestDependencyType dependencyType, ModuleInfo moduleInfo);
    }
}