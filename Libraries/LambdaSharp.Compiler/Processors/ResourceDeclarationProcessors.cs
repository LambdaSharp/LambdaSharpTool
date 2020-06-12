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
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Processors {
    using ErrorFunc = Func<string, Error>;

    internal sealed class ResourceDeclarationProcessor : AProcessor {

        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly Error IfAttributeRequiresCloudFormationType = new Error(0, "'If' attribute can only be used with a CloudFormation type");
        private static readonly Error PropertiesAttributeRequiresCloudFormationType = new Error(0, "'Properties' attribute can only be used with a CloudFormation type");
        private static readonly ErrorFunc ResourceUnknownType = parameter => new Error(0, $"unknown resource type '{parameter}'");
        private static readonly Error TypeAttributeMissing = new Error(0, "'Type' attribute is required");
        private static readonly Error ResourceValueAttributeInvalid = new Error(0, "'Value' attribute must be a valid ARN or wildcard");
        #endregion

        //--- Constructors ---
        public ResourceDeclarationProcessor(IProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Process(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.InspectType<ResourceDeclaration>(node => {

                // check if declaration is a resource reference
                if(node.Value != null) {

                    // referenced resource cannot be conditional
                    if(node.If != null) {
                        Logger.Log(IfAttributeRequiresCloudFormationType, node.If);
                    }

                    // referenced resource cannot have properties
                    if(node.Properties != null) {
                        Logger.Log(PropertiesAttributeRequiresCloudFormationType, node.Properties);
                    }

                    // validate Value attribute
                    if(node.Value is ListExpression listExpression) {
                        foreach(var arnValue in listExpression) {
                            ValidateARN(arnValue);
                        }

                        // default type to 'List'
                        if(node.Type == null) {

                            // TODO: what's the best type here?
                            node.Type = Fn.Literal("List");
                        }
                    } else {
                        ValidateARN(node.Value);

                        // default type to 'String'
                        if(node.Type == null) {
                            node.Type = Fn.Literal("String");
                        }
                    }
                } else if(node.Type != null) {

                    // ensure Properties property is set to an empty object expression when null
                    if(node.Properties == null) {
                        node.Properties = new ObjectExpression();
                    }

                    // NOTE (2020-06-12, bjorg): resource initialization is checked by ResourceInitializationValidator

                    // nothing further to do
                } else {

                    // CloudFormation resource must have a type
                    Logger.Log(TypeAttributeMissing, node);
                }
            });

            // local functions
            void ValidateARN(AExpression arn) {
                if(
                    !(arn is LiteralExpression literalExpression)
                    || (
                        !literalExpression.Value.StartsWith("arn:", StringComparison.Ordinal)
                        && (literalExpression.Value != "*")
                    )
                ) {
                    Logger.Log(ResourceValueAttributeInvalid, arn);
                }
            }
        }
    }
}