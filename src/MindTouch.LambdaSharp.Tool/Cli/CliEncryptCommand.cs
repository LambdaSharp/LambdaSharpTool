/*
 * MindTouch λ#
 * Copyright (C) 2006-2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.SimpleSystemsManagement;
using McMaster.Extensions.CommandLineUtils;
using MindTouch.LambdaSharp.Tool.Internal;

namespace MindTouch.LambdaSharp.Tool.Cli {

    public class CliEncryptCommand : ACliCommand {

        //--- Methods ---
        public void Register(CommandLineApplication app) {
            app.Command("encrypt", cmd => {
                cmd.HelpOption();
                cmd.Description = "Encrypt with Default LambdaSharp Secrets Key";
                var keyOption = cmd.Option("--key <KEY-ID>", "Specify encryption key ot use", CommandOptionType.SingleValue);
                var tierOption = cmd.Option("--tier|-T <NAME>", "Name of deployment tier (default: LAMBDASHARP_TIER environment variable)", CommandOptionType.SingleValue);
                var valueArgument = cmd.Argument("<VALUE>", "Value to encrypt");

                // command options
                cmd.OnExecute(async () => {
                    Console.WriteLine($"{app.FullName} - {cmd.Description}");
                    var keyId = keyOption.Value();
                    var tier = tierOption.Value() ?? Environment.GetEnvironmentVariable("LAMBDASHARP_TIER");
                    if((keyId == null) && (tier != null)) {
                        keyId = $"alias/{tier}-LambdaSharpDefaultSecretKey";
                    }

                    // check if a key id was provided
                    if(keyId == null) {
                        AddError("must provide a key id with --key");
                        return;
                    }
                    var result = await EncryptAsync(
                        keyId,
                        valueArgument.Value
                    );
                    Console.WriteLine();
                    Console.WriteLine(result);
                });
            });
        }

        public async Task<string> EncryptAsync(string keyId, string text) {
            var kmsClient = new AmazonKeyManagementServiceClient();
            var response = await kmsClient.EncryptAsync(new EncryptRequest {
                KeyId = keyId,
                Plaintext = new MemoryStream(Encoding.UTF8.GetBytes(text)),
                EncryptionContext = null
            });
            return Convert.ToBase64String(response.CiphertextBlob.ToArray());
        }
    }
}
