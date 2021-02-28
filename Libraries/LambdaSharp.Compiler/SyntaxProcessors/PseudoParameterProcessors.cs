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
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    /// <summary>
    /// The <see cref="PseudoParameterProcessor"/> class adds declarations for the built-in CloudFormation pseudo-parameters.
    /// </summary>
    internal sealed class PseudoParameterProcessor : ASyntaxProcessor {

        //--- Constructors ---
        public PseudoParameterProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Declare(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.Items.Add(new GroupDeclaration(Fn.Literal("AWS")) {
                Description = Fn.Literal("AWS Pseudo-Parameters"),
                Items = {
                    new PseudoParameterDeclaration(Fn.Literal("AccountId")) {
                        Description = Fn.Literal("AWS account ID of the account in which the CloudFormation stack is being created")
                    },
                    new PseudoParameterDeclaration(Fn.Literal("NotificationARNs")) {
                        Description = Fn.Literal("List of notification Amazon Resource Names (ARNs) for the current CloudFormation stack")
                    },
                    new PseudoParameterDeclaration(Fn.Literal("NoValue")) {
                        Description = Fn.Literal("Removes the resource property it is assigned to")
                    },
                    new PseudoParameterDeclaration(Fn.Literal("Partition")) {
                        Description = Fn.Literal("Partition that the resource is in (e.g. aws, aws-cn, aws-us-gov, etc.)")
                    },
                    new PseudoParameterDeclaration(Fn.Literal("Region")) {
                        Description = Fn.Literal("AWS Region in which the CloudFormation stack is located")
                    },
                    new PseudoParameterDeclaration(Fn.Literal("StackId")) {
                        Description = Fn.Literal("ARN of the current CloudFormation stack")
                    },
                    new PseudoParameterDeclaration(Fn.Literal("StackName")) {
                        Description = Fn.Literal("Name of the current CloudFormation stack")
                    },
                    new PseudoParameterDeclaration(Fn.Literal("URLSuffix")) {
                        Description = Fn.Literal("Suffix for a domain (e.g. amazonaws.com, amazonaws.com.cn, etc.)")
                    }
                }
            });
        }
    }
}