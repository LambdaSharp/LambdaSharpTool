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
    using ErrorFunc = Func<string, Error>;

    /// <summary>
    /// The <see cref="ResourceDeclarationProcessor"/> class
    /// </summary>
    internal sealed class ResourceDeclarationProcessor : ASyntaxProcessor {

        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly Error IfAttributeRequiresCloudFormationType = new Error("'If' attribute can only be used with a CloudFormation type");
        private static readonly Error PropertiesAttributeRequiresCloudFormationType = new Error("'Properties' attribute can only be used with a CloudFormation type");
        private static readonly ErrorFunc ResourceUnknownType = parameter => new Error($"unknown resource type '{parameter}'");
        private static readonly Error TypeAttributeMissing = new Error("'Type' attribute is required");
        private static readonly Error ResourceValueAttributeInvalid = new Error("'Value' attribute must be a valid ARN or wildcard");
        private static readonly ErrorFunc UnknownType = parameter => new Error($"unknown parameter type '{parameter}'");
        #endregion

        //--- Constructors ---
        public ResourceDeclarationProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void ValidateDeclaration(ModuleDeclaration moduleDeclaration) {
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

                    // check if type name exists
                    if(Provider.TryGetResourceType(node.Type.Value, out _)) {
                        Logger.Log(UnknownType(node.Type.Value), node.Type);
                    }

                    // ensure Properties property is set to an empty object expression when null
                    if(node.Properties == null) {
                        node.Properties = new ObjectExpression();
                    }

                    // NOTE (2020-06-12, bjorg): resource initialization is checked later by ResourceInitializationValidator;
                    //  it cannot be checked here because not all declarations have been processed yet.
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