/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2022
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

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using LambdaSharp;

namespace Test.TestModule.MyFunction {

    public class FunctionRequest { }
    public class FunctionResponse { }

    public sealed class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Fields ---
        private string? _secretArn;
        private IAmazonSecretsManager? _secretManagerClient;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Properties ---
        private string SecretArn => _secretArn ?? throw new InvalidOperationException();
        private IAmazonSecretsManager SecretManagerClient => _secretManagerClient ?? throw new InvalidOperationException();

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            _secretArn = config.ReadText("CredentialsSecret");

            // create clients
            _secretManagerClient = new AmazonSecretsManagerClient();
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {
            LogInfo("retrieving secret");
            var secret = await SecretManagerClient.GetSecretValueAsync(new GetSecretValueRequest {
                SecretId = SecretArn
            });
            LogInfo($"Received: {secret.SecretString}");
            return new FunctionResponse();
        }
    }
}
