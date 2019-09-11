/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using McMaster.Extensions.CommandLineUtils;

namespace LambdaSharp.Tool.Cli {

    public class CliEncryptCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("encrypt", cmd => {
                cmd.HelpOption();
                cmd.Description = "Encrypt Value";
                var keyOption = cmd.Option("--key <KEY-ID>", "Specify encryption key ID or alias to use", CommandOptionType.SingleValue);
                var decryptOption = cmd.Option("--decrypt", "(optional) Decrypt value before encrypting it.", CommandOptionType.NoValue);
                var valueArgument = cmd.Argument("<VALUE>", "Value to encrypt");

                // command options
                var initSettingsCallback = CreateSettingsInitializer(cmd);
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var settings = await initSettingsCallback();
                    if(settings == null) {
                        return;
                    }

                    // either use an explicitly provided key ID or use the default key for the deployment
                    var keyId = keyOption.Value();
                    if(keyId == null) {
                        LogError("must provide a key ARN or alias with --key");
                        return;
                    }

                    // if no argument is provided, read text from standard in
                    var text = valueArgument.Value;
                    if(text == null) {
                        var builder = new StringBuilder();
                        for(var input = Console.ReadLine(); input != null; input = Console.ReadLine()) {
                            builder.AppendLine(input);
                        }
                        text = builder.ToString();
                    }

                    var result = await EncryptAsync(
                        keyId,
                        text,
                        decryptOption.HasValue()
                    );
                    Console.WriteLine();
                    Console.WriteLine(result);
                });
            });
        }

        public async Task<string> EncryptAsync(string keyId, string text, bool decrypt) {
            var kmsClient = new AmazonKeyManagementServiceClient();

            // check if value needs to be decrypted first
            if(decrypt) {
                var decryptResponse = await kmsClient.DecryptAsync(new DecryptRequest {
                    CiphertextBlob = new MemoryStream(Convert.FromBase64String(text))
                });
                text = Encoding.UTF8.GetString(decryptResponse.Plaintext.ToArray());
            }

            // encrypt text value
            var encryptResponse = await kmsClient.EncryptAsync(new EncryptRequest {
                KeyId = keyId,
                Plaintext = new MemoryStream(Encoding.UTF8.GetBytes(text)),
                EncryptionContext = null
            });
            return Convert.ToBase64String(encryptResponse.CiphertextBlob.ToArray());
        }
    }
}
