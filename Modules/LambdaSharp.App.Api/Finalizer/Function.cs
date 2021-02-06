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

using System.Linq;
using System.Threading.Tasks;
using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using LambdaSharp.Finalizer;

namespace LambdaSharp.AppHosting.Finalizer {

    public sealed class Function : ALambdaFinalizerFunction {

        //--- Fields ---
        private IAmazonAPIGateway _apiGatewayClient;
        private string _restApiId;
        private string _stageName;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            _restApiId = config.ReadText("RestApi");
            _stageName = config.ReadText("RestApiStage");

            // initialize clients
            _apiGatewayClient = new AmazonAPIGatewayClient();
        }

        public override Task CreateDeployment(FinalizerProperties current) => Task.CompletedTask;
        public override Task UpdateDeployment(FinalizerProperties current, FinalizerProperties previous) => UpdateApiDeployment();
        public override Task DeleteDeployment(FinalizerProperties current) => DeleteApiDeployments();

        private async Task UpdateApiDeployment() {

            // list current deployments
            LogInfo("Listing deployments");
            var deploymentsResponse = await  _apiGatewayClient.GetDeploymentsAsync(new GetDeploymentsRequest {
                RestApiId = _restApiId
            });
            LogInfo($"Found {deploymentsResponse.Items.Count:N0} deployments");

            // create a new deployment
            LogInfo("Creating a new deployment");
            var createResponse = await _apiGatewayClient.CreateDeploymentAsync(new CreateDeploymentRequest {
                RestApiId = _restApiId,
                Description = $"{Info.ModuleId} LambdaSharp App API (update)"
            });
            LogInfo($"New deployment created: id={createResponse.Id}");

            // update stage to use new deployment
            LogInfo("Updating stage");
            await _apiGatewayClient.UpdateStageAsync(new UpdateStageRequest {
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
                    await _apiGatewayClient.DeleteDeploymentAsync(new DeleteDeploymentRequest {
                        RestApiId = _restApiId,
                        DeploymentId = deployment.Id
                    });
                }
            }
        }

        private async Task DeleteApiDeployments() {

            // remove old deployments
            LogInfo("Listing deployments");
            var deploymentsResponse = await  _apiGatewayClient.GetDeploymentsAsync(new GetDeploymentsRequest {
                RestApiId = _restApiId
            });
            LogInfo($"Found {deploymentsResponse.Items.Count:N0} deployments");

            // update stage to use the oldest (first) deployment
            LogInfo("Updating stage to use first deployment");
            await _apiGatewayClient.UpdateStageAsync(new UpdateStageRequest {
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
                    await _apiGatewayClient.DeleteDeploymentAsync(new DeleteDeploymentRequest {
                        RestApiId = _restApiId,
                        DeploymentId = deployment.Id
                    });
                }
            }
        }
    }
}
