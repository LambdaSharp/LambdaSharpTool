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

namespace LambdaSharp.Compiler.Validators {

    internal sealed class AllowValidator : AValidator {

        //--- Constructors ---
        public AllowValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration) {

            // TODO: validate allow statements
            // TODO: add allow statements to IAM role
            //  - resource dynamic
            //  - resource fixed
            //  - list of fixed resources
            //  - parameter

            throw new NotImplementedException();
        }
    }
}