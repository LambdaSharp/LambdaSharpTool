/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using MindTouch.LambdaSharp;
using MindTouch.LambdaSharp.CustomResource;
using MindTouch.Rollbar;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MindTouch.LambdaSharpRollbar.ResourceHandler {

    public class RequestProperties {

        //--- Properties ---
        public string Project { get; set; }
        public string Deployment { get; set; }
    }

    public class ResponseProperties {

        //--- Properties ---
        public string Token { get; set; }
        public string Result { get; set; }
    }

    public class RollbarResourceException : ARollbarException {

        //--- Constructors ---
        public RollbarResourceException(string format, params object[] args) : base(format, args) { }
    }

    public class Function : ALambdaCustomResourceFunction<RequestProperties, ResponseProperties> {

        //--- Class Methods ---
        private static string ToPhysicalResourceId(int projectId) => $"rollbar:project:{projectId}";

        private static int FromPhysicalResourceId(string physicalResourceId) {
            const string prefix = "rollbar:project:";
            if(!physicalResourceId.StartsWith(prefix)) {
                throw new Exception("invalid physical resource id: " + physicalResourceId);
            }
            return int.Parse(physicalResourceId.Substring(prefix.Length));
        }

        //--- Fields ---
        private string _accountWriteAccessToken;
        private string _accountReadAccessToken;

        //--- Methods ---
        public override Task InitializeAsync(LambdaConfig config) {
            _accountWriteAccessToken = config.ReadText("WriteAccessToken");
            _accountReadAccessToken = config.ReadText("ReadAccessToken");
            return Task.CompletedTask;
        }

        protected async override Task<Response<ResponseProperties>> HandleCreateResourceAsync(Request<RequestProperties> request) {
            if(request?.ResourceProperties?.Deployment == null) {
                throw new ArgumentNullException(nameof(request.ResourceProperties.Deployment));
            }
            if(request?.ResourceProperties?.Project == null) {
                throw new ArgumentNullException(nameof(request.ResourceProperties.Project));
            }

            // create new rollbar project
            var name = $"{request.ResourceProperties.Deployment}-{request.ResourceProperties.Project}";
            var project = await CreateProject(name);
            var tokens = await ListProjectTokens(project.Id);
            var token = tokens.First(t => t.Name == "post_server_item").AccessToken;
            return new Response<ResponseProperties> {
                PhysicalResourceId = $"rollbar:project:{project.Id}",
                NoEcho = true,
                Properties = new ResponseProperties {
                    Token = token,
                    Result = token
                }
            };
        }

        protected async override Task<Response<ResponseProperties>> HandleUpdateResourceAsync(Request<RequestProperties> request) {
            if(request?.ResourceProperties?.Deployment == null) {
                throw new ArgumentNullException(nameof(request.ResourceProperties.Deployment));
            }
            if(request?.ResourceProperties?.Project == null) {
                throw new ArgumentNullException(nameof(request.ResourceProperties.Project));
            }

            // if name is same then nothing has changed
            var name = $"{request.ResourceProperties.Deployment}-{request.ResourceProperties.Project}";
            var projectId = FromPhysicalResourceId(request.PhysicalResourceId);
            var project = await GetProject(projectId);
            if(project.Name == name) {
                return new Response<ResponseProperties> {
                    PhysicalResourceId = request.PhysicalResourceId
                };
            }

            // create new project with new name
            project = await CreateProject(name);
            var tokens = await ListProjectTokens(project.Id);
            var token = tokens.First(t => t.Name == "post_server_item").AccessToken;
            return new Response<ResponseProperties> {
                PhysicalResourceId = $"rollbar:project:{project.Id}",
                NoEcho = true,
                Properties = new ResponseProperties {
                    Token = token,
                    Result = token
                }
            };
        }

        protected async override Task<Response<ResponseProperties>> HandleDeleteResourceAsync(Request<RequestProperties> request) {
            if(request?.PhysicalResourceId == null) {
                throw new ArgumentNullException(nameof(request.PhysicalResourceId));
            }

            // delete old rollbar project
            try {
                var projectId = FromPhysicalResourceId(request.PhysicalResourceId);
                await DeleteProject(projectId);
            } catch(Exception e) {
                LogErrorAsWarning(e, "failed to delete project: PhysicalResourceId={0}", request.PhysicalResourceId);
            }
            return new Response<ResponseProperties>();
        }

        private async Task<RollbarProject> CreateProject(string projectName) {
            LogInfo($"create rollbar project {projectName}");
            var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                RequestUri = new Uri("https://api.rollbar.com/api/1/projects/"),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(new RollbarCreateProjectRequest {
                    AccessToken = _accountWriteAccessToken,
                    Name = projectName
                }), Encoding.UTF8, "application/json")
            });
            var result = JsonConvert.DeserializeObject<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
            if((httpResponse.StatusCode == (HttpStatusCode)422) && (result.Message == "Project with this name already exists")) {
                var allProjects = await ListAllProjects();
                return allProjects.First(project => projectName.Equals(project.Name, StringComparison.InvariantCultureIgnoreCase));
            }
            if(!httpResponse.IsSuccessStatusCode) {
                throw new Exception($"http operation failed: {httpResponse.StatusCode}");
            }
            if(result.Error != 0) {
                throw new Exception($"rollbar operation failed: {result.Message}");
            }
            return JsonConvert.DeserializeObject<RollbarProject>(JsonConvert.SerializeObject(result.Result));
        }

        private async Task<IEnumerable<RollbarProject>> ListAllProjects() {
            LogInfo($"list all rollbar projects");
            var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                RequestUri = new Uri($"https://api.rollbar.com/api/1/projects/?access_token={_accountReadAccessToken}"),
                Method = HttpMethod.Get
            });
            if(!httpResponse.IsSuccessStatusCode) {
                throw new Exception($"http operation failed: {httpResponse.StatusCode}");
            }
            var result = JsonConvert.DeserializeObject<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
            if(result.Error != 0) {
                throw new Exception($"rollbar operation failed: {result.Message}");
            }
            var list = JsonConvert.DeserializeObject<List<RollbarProject>>(JsonConvert.SerializeObject(result.Result));
            return list.Where(project => project.Name != null).ToArray();
        }

        private async Task<RollbarProject> GetProject(int projectId) {
            LogInfo($"get rollbar project {projectId}");
            var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                RequestUri = new Uri($"https://api.rollbar.com/api/1/project/{projectId}?access_token={_accountReadAccessToken}"),
                Method = HttpMethod.Get
            });
            if(!httpResponse.IsSuccessStatusCode) {
                throw new Exception($"http operation failed: {httpResponse.StatusCode}");
            }
            var result = JsonConvert.DeserializeObject<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
            if(result.Error != 0) {
                throw new Exception($"rollbar operation failed: {result.Message}");
            }
            return JsonConvert.DeserializeObject<RollbarProject>(JsonConvert.SerializeObject(result.Result));
        }

        private async Task DeleteProject(int projectId) {
            LogInfo($"delete rollbar project {projectId}");
            var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                RequestUri = new Uri($"https://api.rollbar.com/api/1/project/{projectId}?access_token={_accountWriteAccessToken}"),
                Method = HttpMethod.Delete
            });
            if(!httpResponse.IsSuccessStatusCode) {
                throw new Exception($"http operation failed: {httpResponse.StatusCode}");
            }
            var result = JsonConvert.DeserializeObject<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
            if(result.Error != 0) {
                throw new Exception($"rollbar operation failed: {result.Message}");
            }
        }

        private async Task<IEnumerable<RollbarProjectToken>> ListProjectTokens(int projectId) {
            LogInfo($"list rollbar project tokens {projectId}");
            var httpResponse = await HttpClient.SendAsync(new HttpRequestMessage {
                RequestUri = new Uri($"https://api.rollbar.com/api/1/project/{projectId}/access_tokens?access_token={_accountReadAccessToken}"),
                Method = HttpMethod.Get
            });
            if(!httpResponse.IsSuccessStatusCode) {
                throw new Exception($"http operation failed: {httpResponse.StatusCode}");
            }
            var result = JsonConvert.DeserializeObject<RollbarResponse>(await httpResponse.Content.ReadAsStringAsync());
            if(result.Error != 0) {
                    throw new Exception($"rollbar operation failed: {result.Message}");
            }
            return JsonConvert.DeserializeObject<List<RollbarProjectToken>>(JsonConvert.SerializeObject(result.Result));
        }
    }
}
