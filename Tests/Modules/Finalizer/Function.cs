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

using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp;
using LambdaSharp.Finalizer;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharpTestModule.Finalizer {

    public class Function : ALambdaFinalizerFunction {

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config)
            => Task.CompletedTask;

        public override async Task CreateDeployment(FinalizerProperties current) {
            LogInfo($"Creating Deployment: {current.DeploymentChecksum}");
        }

        public override async Task UpdateDeployment(FinalizerProperties next, FinalizerProperties previous) {
            LogInfo($"Updating Deployment: {previous.DeploymentChecksum} -> {next.DeploymentChecksum}");
        }

        public override async Task DeleteDeployment(FinalizerProperties current) {
            LogInfo($"Deleting Deployment: {current.DeploymentChecksum}");
        }
    }
}
