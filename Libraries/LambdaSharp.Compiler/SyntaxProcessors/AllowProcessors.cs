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
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal sealed class AllowProcessor : ASyntaxProcessor {

        //--- Constructors ---
        public AllowProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration) {

            // TODO: generalize this to IAllowDeclaration

            // TODO: validate allow statements
            //  - check if the allowed operations are valid for the specified type
            //  - special type 'AWS' allows any valid permission (i.e. permissions can be mixed)
            // TODO: add allow statements to IAM role
            //  - resource dynamic
            //  - resource fixed
            //  - list of fixed resources
            //  - parameter

            // if(allow != null) {
            //     if(type == null) {
            //         _builder.Log(Error.AllowAttributeRequiresTypeAttribute, node);
            //     } else if(type?.Value == "AWS") {

            //         // nothing to do; any 'Allow' expression is legal
            //     } else if(!IsValidCloudFormationResourceType(type.Value)) {
            //         _builder.Log(Error.AllowAttributeRequiresCloudFormationType, node);
            //     } else {

            //         // TODO: ResourceMapping.IsCloudFormationType(node.Type?.Value), "'Allow' attribute can only be used with AWS resource types"
            //     }
            // }


            throw new NotImplementedException();
        }
    }
}