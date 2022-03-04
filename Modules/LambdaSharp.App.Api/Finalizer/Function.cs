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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using LambdaSharp.Finalizer;

namespace LambdaSharp.AppHosting.Finalizer {

    public sealed class Function : ALambdaFinalizerFunction {

        //--- Fields ---
        private IAmazonAPIGateway? _apiGatewayClient;
        private string? _restApiId;
        private string? _stageName;

        //--- Properties ---
        private IAmazonAPIGateway ApiGatewayClient => _apiGatewayClient ?? throw new InvalidOperationException();

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            _restApiId = config.ReadText("RestApi");
            _stageName = config.ReadText("RestApiStage");

            // initialize clients
            _apiGatewayClient = new AmazonAPIGatewayClient();
        }

        public override Task CreateDeploymentAsync(FinalizerProperties current, CancellationToken cancellationToken) => Task.CompletedTask;
        public override Task UpdateDeploymentAsync(FinalizerProperties current, FinalizerProperties previous, CancellationToken cancellationToken) => UpdateApiDeploymentAsync(cancellationToken);
        public override Task DeleteDeploymentAsync(FinalizerProperties current, CancellationToken cancellationToken) => DeleteApiDeploymentAsync(cancellationToken);

        private async Task UpdateApiDeploymentAsync(CancellationToken cancellationToken) {

            // list current deployments
            LogInfo("Listing deployments");
            var deploymentsResponse = await ApiGatewayClient.GetDeploymentsAsync(new GetDeploymentsRequest {
                RestApiId = _restApiId
            });
            LogInfo($"Found {deploymentsResponse.Items.Count:N0} deployments");

            // create a new deployment
            LogInfo("Creating a new deployment");
            var createResponse = await ApiGatewayClient.CreateDeploymentAsync(new CreateDeploymentRequest {
                RestApiId = _restApiId,
                Description = $"{Info.ModuleId} LambdaSharp App API (update)"
            });
            LogInfo($"New deployment created: id={createResponse.Id}");

            // update stage to use new deployment
            LogInfo("Updating stage");
            await ApiGatewayClient.UpdateStageAsync(new UpdateStageRequest {
                RestApiId = _restApiId,
                StageName = _stageName,
                PatchOperations = {
                    new PatchOperation {
                        Op = Op.Replace,
                        Path = "/deploymentId",
                        Value = createResponse.Id
                    }
                }
            });

            // remove all previous deployments, except one oldest which is managed by CloudFormation
            var deploymentsToDelete = deploymentsResponse.Items.OrderByDescending(deployment => deployment.CreatedDate).SkipLast(1).ToList();
            if(deploymentsToDelete.Any()) {
                LogInfo("Deleting old deployments");
                foreach(var deployment in deploymentsToDelete) {
                    LogInfo($"Deleting deployment: id={deployment.Id}");
                    await ApiGatewayClient.DeleteDeploymentAsync(new DeleteDeploymentRequest {
                        RestApiId = _restApiId,
                        DeploymentId = deployment.Id
                    });
                }
            }
        }

        private async Task DeleteApiDeploymentAsync(CancellationToken cancellationToken) {

            // remove old deployments
            LogInfo("Listing deployments");
            var deploymentsResponse = await ApiGatewayClient.GetDeploymentsAsync(new GetDeploymentsRequest {
                RestApiId = _restApiId
            });
            LogInfo($"Found {deploymentsResponse.Items.Count:N0} deployments");

            // update stage to use the oldest (first) deployment
            LogInfo("Updating stage to use first deployment");
            await ApiGatewayClient.UpdateStageAsync(new UpdateStageRequest {
                RestApiId = _restApiId,
                StageName = _stageName,
                PatchOperations = {
                    new PatchOperation {
                        Op = Op.Replace,
                        Path = "/deploymentId",
                        Value = deploymentsResponse.Items.OrderByDescending(deployment => deployment.CreatedDate).Last().Id
                    }
                }
            });

            // remove all deployments, except oldest one which is managed by CloudFormation
            var deploymentsToDelete = deploymentsResponse.Items.OrderByDescending(deployment => deployment.CreatedDate).SkipLast(1).ToList();
            if(deploymentsToDelete.Any()) {
                LogInfo("Deleting old deployments");
                foreach(var deployment in deploymentsToDelete) {
                    LogInfo($"Deleting deployment: id={deployment.Id}");
                    await ApiGatewayClient.DeleteDeploymentAsync(new DeleteDeploymentRequest {
                        RestApiId = _restApiId,
                        DeploymentId = deployment.Id
                    });
                }
            }
        }
    }
}
