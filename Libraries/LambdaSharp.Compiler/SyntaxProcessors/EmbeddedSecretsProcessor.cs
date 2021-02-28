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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal sealed class EmbeddedSecretsProcessor : ASyntaxProcessor {

        //--- Class Fields ---
        private static Regex SecretArnRegex = new Regex(@"^arn:aws:kms:[a-z\-]+-\d:\d{12}:key\/[a-fA-F0-9\-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Regex SecretAliasRegex = new Regex("^[0-9a-zA-Z/_\\-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        #region Errors/Warnings
        private static readonly Error SecretKeyMustBeValidARN = new Error("secret key must be a valid ARN");
        private static readonly Error CannotGrantPermissionToDecryptParameterStore = new Error("cannot grant permission to decrypt with aws/ssm");
        private static readonly Error SecreteKeyMustBeValidAlias = new Error("secret key must be a valid alias");
        private static readonly Error UnableToResolveAlias = new Error("cannot resolve secret key from alias to ARN");
        #endregion

        //--- Constructors ---
        public EmbeddedSecretsProcessor(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public async Task ProcessAsync(ModuleDeclaration moduleDeclaration) {
            await moduleDeclaration.InspectAsync(async node => {
                switch(node) {
                case ModuleDeclaration innerModuleDeclaration:
                    await ValidateSecretsAsync(moduleDeclaration);
                    break;
                }
            });

            // local functions
            async Task ValidateSecretsAsync(ModuleDeclaration node) {
                for(var i = 0; i < node.Secrets.Count; ++i) {
                    var secret = node.Secrets[i];
                    if(secret.Value.Equals("aws/ssm", StringComparison.OrdinalIgnoreCase)) {

                        // the parameter store encryption key cannot be used
                        Logger.Log(CannotGrantPermissionToDecryptParameterStore, secret);
                    } else if(secret.Value.StartsWith("arn:", StringComparison.Ordinal)) {

                        // only allow ARNs that describe KMS keys
                        if(!SecretArnRegex.IsMatch(secret.Value)) {
                            Logger.Log(SecretKeyMustBeValidARN, secret);
                        }
                    } else if(SecretAliasRegex.IsMatch(secret.Value)) {

                        // convert KMS alias to ARN
                        try {
                            var newSecretValue = await Provider.ConvertKmsAliasToArn(secret.Value);
                            node.Secrets[i] = Fn.Literal(newSecretValue, secret.SourceLocation);
                        } catch {
                            Logger.Log(UnableToResolveAlias, secret);
                        }
                    } else {
                        Logger.Log(SecreteKeyMustBeValidAlias, secret);
                    }
                }
            }
        }
    }
}