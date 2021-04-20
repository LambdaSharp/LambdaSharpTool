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
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LambdaSharp.ConfigSource;

namespace LambdaSharp.Debug {

    public class DebugLambdaFunctionDependencyProvider : ILambdaFunctionDependencyProvider {

        //--- Fields ---
        private readonly ILambdaConfigSource _configSource;
        private readonly HttpClient _httpClient = new HttpClient();

        //--- Constructors ---
        public DebugLambdaFunctionDependencyProvider(Dictionary<string, string> environmentVariables) {
            _configSource = new LambdaDictionaryEnvironmentSource(environmentVariables ?? throw new ArgumentNullException(nameof(environmentVariables)));
        }

        //--- Properties ---
        public DateTime UtcNow => DateTime.UtcNow;
        public ILambdaConfigSource ConfigSource => _configSource;
        public bool DebugLoggingEnabled => true;
        public HttpClient HttpClient => _httpClient;

        //--- Methods ---
        public Task<byte[]> DecryptSecretAsync(byte[] secretBytes, Dictionary<string, string> encryptionContext = null, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<byte[]> EncryptSecretAsync(byte[] plaintextBytes, string encryptionKeyId, Dictionary<string, string> encryptionContext = null, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public void Log(string message) => Console.Write($"LAMBDA => {message.Replace("\r", "").Replace("\n", Environment.NewLine)}");

        public Task<string?> ReadTextFromFile(string filepath) => Task.FromResult((string?)null);

        public Task SendEventAsync(DateTimeOffset timestamp, string eventbus, string source, string detailType, string detail, IEnumerable<string> resources, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task SendMessageToQueueAsync(string queueUrl, string message, IEnumerable<KeyValuePair<string, string>>? messageAttributes = null, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
    }
}
