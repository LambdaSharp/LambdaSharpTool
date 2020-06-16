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

using System.Linq;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal sealed class SecretTypeDeclarationProcessor : ASyntaxProcessor {


        //--- Class Fields ---

        #region Errors/Warnings
        private static readonly Error EncryptionContextExpectedLiteralStringExpression = new Error(0, "'EncryptionContext' expected literal string expression");
        private static readonly Error EncryptionContextAttributeRequiresSecretType = new Error(0, "'EncryptionContext' attribute can only be used with 'Secret' type");
        #endregion

        //--- Constructors --
        public SecretTypeDeclarationProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Process(ModuleDeclaration moduleDeclaration) {
            moduleDeclaration.Inspect(node => {
                switch(node) {
                case ParameterDeclaration parameterDeclaration:
                    if(parameterDeclaration.Type?.Value == "Secret") {
                        ValidateEncryptContext(parameterDeclaration.EncryptionContext);
                    } else if(parameterDeclaration.EncryptionContext != null) {
                        Logger.Log(EncryptionContextAttributeRequiresSecretType, parameterDeclaration.EncryptionContext);
                    }
                    break;
                case ImportDeclaration importDeclaration:
                    if(importDeclaration.Type?.Value == "Secret") {
                        ValidateEncryptContext(importDeclaration.EncryptionContext);
                    } else if(importDeclaration.EncryptionContext != null) {
                        Logger.Log(EncryptionContextAttributeRequiresSecretType, importDeclaration.EncryptionContext);
                    }
                    break;
                case VariableDeclaration variableDeclaration:
                    if(variableDeclaration.Type?.Value == "Secret") {
                        ValidateEncryptContext(variableDeclaration.EncryptionContext);
                    } else if(variableDeclaration.EncryptionContext != null) {
                        Logger.Log(EncryptionContextAttributeRequiresSecretType, variableDeclaration.EncryptionContext);
                    }
                    break;
                }
            });

            // local functions
            void Foo(AItemDeclaration parent, ObjectExpression? encryptionContext) {

                // set declaration expression
                AExpression declarationExpression;
                if(encryptionContext != null) {
                    declarationExpression = Fn.Join(
                        "|",
                        new AExpression[] {
                            Fn.Ref(parent.FullName)
                        }.Union(
                            encryptionContext.Select(kv => Fn.Literal($"{kv.Key}={kv.Value}"))
                        ).ToArray()
                    );
                } else {
                    declarationExpression = Fn.Ref(parent.FullName);
                }

                // add resource for decrypting secret
                var decoderResourceDeclaration = new ResourceDeclaration(Fn.Literal("Decoder")) {

                    // TODO: let's register this as a local custom resource type!
                    Type = Fn.Literal("Module::DecryptSecret"),
                    Properties = {
                        ["Ciphertext"] = Fn.Ref(parent.FullName)
                    },
                    DiscardIfNotReachable = true
                };
                parent.Adopt(decoderResourceDeclaration);
                Provider.DeclareItem(decoderResourceDeclaration);

                // add variable to retrieve decrypted secret
                var plaintextVariableDeclaration = new VariableDeclaration(Fn.Literal("Plaintext")) {
                    Value = Fn.GetAtt(decoderResourceDeclaration.FullName, "Plaintext")
                };
                parent.Adopt(plaintextVariableDeclaration);
                Provider.DeclareItem(plaintextVariableDeclaration);
            }

        }

        private void ValidateEncryptContext(ObjectExpression? encryptionContext) {

            // all 'EncryptionContext' values must be literal values
            if(encryptionContext != null) {

                // all expressions must be literals for the EncryptionContext
                foreach(var kv in encryptionContext) {
                    if(!(kv.Value is LiteralExpression)) {
                        Logger.Log(EncryptionContextExpectedLiteralStringExpression, kv.Value);
                    }
                }
            }
        }
    }
}